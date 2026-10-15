using System.Net;
using System.Text;
using Microsoft.Net.Http.Headers;
using Scout.Web.Storage;
namespace Scout.Web.Features.Documents;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        app.MapGet("/documents/{id:int}/download", (int id, HttpContext context, DocumentOperations documents, DocumentFileStore files) => Read(id, false, context, documents, files));
        app.MapGet("/documents/{id:int}/view", (int id, HttpContext context, DocumentOperations documents, DocumentFileStore files) => Read(id, true, context, documents, files));
    }
    private static async Task<IResult> Read(int id, bool preview, HttpContext context, DocumentOperations documents, DocumentFileStore files)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; base-uri 'none'; form-action 'none'; frame-ancestors 'self'; sandbox allow-downloads";
        try
        {
            var d = await documents.GetAsync(id);
            if (d is null)
                return Results.Text("Document not found.", statusCode: 404);
            if (preview && !DocumentFileStore.CanPreview(d))
                return Results.Text("This file supports download only. Return to its document page.", statusCode: 400);
            var stream = await files.OpenVerifiedAsync(d, context.RequestAborted);
            if (preview && d.ContentType == "text/plain")
            {
                await using (stream)
                {
                    var bytes = new byte[(int)Math.Min(stream.Length, DocumentFileStore.PreviewBytes)];
                    await stream.ReadExactlyAsync(bytes, context.RequestAborted);
                    using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    var text = await reader.ReadToEndAsync(context.RequestAborted);
                    var html = $"<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><title>{WebUtility.HtmlEncode(d.OriginalName)}</title><h1>{WebUtility.HtmlEncode(d.OriginalName)}</h1>{(stream.Length > bytes.Length ? "<p>Preview truncated to the first 256 KiB. Download to read the full file.</p>" : "")}<p><a href=\"/documents/{id}/download\">Download original</a></p><pre>{WebUtility.HtmlEncode(text)}</pre></html>";
                    return Results.Content(html, "text/html; charset=utf-8");
                }
            }
            if (preview)
            {
                var disposition = new ContentDispositionHeaderValue("inline");
                disposition.SetHttpFileName(d.OriginalName);
                context.Response.Headers.ContentDisposition = disposition.ToString();
                return Results.File(stream, d.ContentType, enableRangeProcessing: false);
            }
            return Results.File(stream, d.ContentType, fileDownloadName: d.OriginalName, enableRangeProcessing: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.DataAnnotations.ValidationException or Microsoft.Data.Sqlite.SqliteException)
        {
            context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("DocumentRead").LogWarning(ex, "Could not read document {DocumentId}.", id);
            return Results.Text("This document could not be read safely. Its file may be missing, changed, or inaccessible. Return to Scout and restore the original file from a backup if necessary.", statusCode: 409);
        }
    }
}
