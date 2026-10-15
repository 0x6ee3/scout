using Microsoft.EntityFrameworkCore;
using Scout.Web.Components;
using Scout.Web.Configuration;
using Scout.Web.Data;
using Scout.Web.Features.Companies;
using Scout.Web.Features.Positions;
using Scout.Web.Features.Documents;
using Scout.Web.Storage;

var builder = WebApplication.CreateBuilder(args);
// Local runs without a launch profile still need the SDK's generated JS/CSS assets.
builder.WebHost.UseStaticWebAssets();
var port = builder.Configuration.GetValue("Scout:Port", 5180);
if (port is < 1 or > 65535)
{
    throw new InvalidOperationException("Scout:Port must be between 1 and 65535.");
}

builder.WebHost.ConfigureKestrel(options =>
{
    // Replace configured endpoints so URL/Kestrel overrides cannot add a LAN listener.
    options.Configure(new ConfigurationBuilder().Build(), reloadOnChange: false);
    options.ListenLocalhost(port);
});

builder.Services.AddSingleton(_ => StoragePaths.ForCurrentUser());
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddDbContextFactory<ScoutDbContext>((services, options) =>
    options.UseSqlite(ScoutDatabase.ConnectionString(services.GetRequiredService<StoragePaths>())));
builder.Services.AddSingleton<DocumentFileStore>();
builder.Services.AddScoped<DocumentOperations>();
builder.Services.AddScoped<Scout.Web.Features.Cvs.CvOperations>();
builder.Services.AddScoped<Scout.Web.Features.Submissions.SubmissionOperations>();
builder.Services.AddScoped<CompanyOperations>();
builder.Services.AddScoped<PositionOperations>();
builder.Services.AddScoped<Scout.Web.Features.Questions.QuestionOperations>();
builder.Services.AddScoped<Scout.Web.Features.TagOperations>();
builder.Services.AddScoped<Scout.Web.Features.Experiences.ExperienceOperations>();
builder.Services.AddScoped<Scout.Web.Features.InterviewPreparation.PreparationOperations>();
builder.Services.AddScoped<Scout.Web.Features.Applications.ApplicationOperations>();
builder.Services.AddScoped<Scout.Web.Features.Contacts.ContactOperations>();
builder.Services.AddScoped<Scout.Web.Features.Timeline.TimelineOperations>();
builder.Services.AddScoped<Scout.Web.Features.Interviews.InterviewOperations>();
builder.Services.AddScoped<Scout.Web.Features.NextActions.NextActionOperations>();

builder.Services.AddScoped<Scout.Web.Features.InterviewWorkspace.WorkspaceOperations>();

var app = builder.Build();
var paths = app.Services.GetRequiredService<StoragePaths>();
app.UseWebSockets();

// A local browser can still be sent to this server by another website.
app.Use(async (context, next) =>
{
    var host = context.Request.Host;
    if (host.Port != port || host.Host.ToLowerInvariant() is not ("localhost" or "127.0.0.1" or "[::1]" or "::1"))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var origin = context.Request.Headers.Origin;
    var expectedOrigin = $"{context.Request.Scheme}://{host}";
    if ((origin.Count > 0 && !string.Equals(origin.ToString(), expectedOrigin, StringComparison.OrdinalIgnoreCase))
        || (context.WebSockets.IsWebSocketRequest && origin.Count == 0))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    await next(context);
});

// Workspace failures must never re-execute a private management page.
app.Use(async (context, next) =>
{
    var segments = context.Request.Path.Value?.Split('/') ?? [];
    if(segments.Length < 4 || !string.Equals(segments[1], "interviews", StringComparison.OrdinalIgnoreCase) || !string.Equals(segments[3], "workspace", StringComparison.OrdinalIgnoreCase) || (segments.Length == 5 && segments[4] == "setup")) { await next(context); return; }
    context.Response.Headers.CacheControl = "private, no-store";
    try { await next(context); }
    catch(Exception) when(!context.Response.HasStarted) { context.Response.Clear(); context.Response.StatusCode = 500; await context.Response.WriteAsync("Interview Workspace unavailable. Close this tab and reopen after private review."); }
});
app.UseWhen(context => !context.Request.Path.Value!.Contains("/workspace", StringComparison.OrdinalIgnoreCase), branch =>
{
    branch.UseExceptionHandler("/error", createScopeForErrors: true);
    branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
});
app.UseAntiforgery();
app.MapStaticAssets();
app.MapDocumentEndpoints();
Scout.Web.Features.InterviewWorkspace.WorkspaceFileEndpoints.MapWorkspaceFileEndpoints(app);
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

try
{
    app.Logger.LogInformation("Scout data directory: {DataRoot}", paths.Root);
    paths.CreateDirectories();
    await using var context = await app.Services.GetRequiredService<IDbContextFactory<ScoutDbContext>>()
        .CreateDbContextAsync();
    await context.Database.MigrateAsync();
    if (await DevelopmentSamples.SeedAsync(context, app.Environment.IsDevelopment(),
        app.Configuration.GetValue<bool>("Development:SeedSampleData"), app.Services.GetRequiredService<DocumentFileStore>()))
        app.Logger.LogInformation("Inserted fictional development samples into an empty Scout database.");
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
    or Microsoft.Data.Sqlite.SqliteException or DbUpdateException or InvalidOperationException)
{
    app.Logger.LogCritical(exception,
        "Scout could not initialize {DatabasePath}. Check directory access and that another Scout process is not using it. No data was reset.",
        paths.DatabasePath);
    throw;
}

await app.RunAsync();

public partial class Program
{
}
