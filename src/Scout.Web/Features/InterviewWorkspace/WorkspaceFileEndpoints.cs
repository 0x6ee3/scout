using Scout.Web.Storage;
namespace Scout.Web.Features.InterviewWorkspace;

public static class WorkspaceFileEndpoints
{
    public static void MapWorkspaceFileEndpoints(this WebApplication app)
    {
        app.MapGet("/interviews/{id:int}/workspace/cv/{action}", async (int id, string action, int? revision, HttpContext context, WorkspaceOperations operations, DocumentFileStore files) =>
        {
            context.Response.Headers.CacheControl = "private, no-store";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; base-uri 'none'; form-action 'none'; frame-ancestors 'self'; sandbox allow-downloads";
            try
            {
                var cv = revision.HasValue ? await operations.CvAsync(id, revision.Value) : null;
                if (cv is null || action is not ("view" or "download"))
                    return Results.Text("Submitted CV unavailable. Private review is required.", statusCode: 404);
                if (cv.ContentType != "application/pdf")
                    return Results.Text("Submitted CV unavailable.", statusCode: 400);
                var stream = await files.OpenVerifiedAsync(cv, context.RequestAborted);
                context.Response.Headers.ContentDisposition = action == "view" ? "inline; filename=submitted-cv.pdf" : "attachment; filename=submitted-cv.pdf";
                return Results.Stream(stream, "application/pdf", enableRangeProcessing: false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return Results.Text("Submitted CV unavailable. Its saved bytes could not be verified.", statusCode: 409);
            }
        });
    }
}
