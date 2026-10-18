using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Features;

namespace Scout.Web.Features.InterviewTranscripts;

public static class TranscriptEndpoints
{
    public static void MapTranscriptEndpoints(this WebApplication app)
    {
        app.MapPost("/interviews/{round:int}/transcripts/{id:int}/correct", Correct);
    }
    private static async Task<IResult> Correct(int round,int id,HttpContext context,IAntiforgery antiforgery,TranscriptOperations operations)
    {
        context.Response.Headers.CacheControl="private, no-store";
        context.Response.Headers.XContentTypeOptions="nosniff";
        var feature=context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if(feature is {IsReadOnly:false}) feature.MaxRequestBodySize=6L*1024*1024;
        try
        {
            var form=await context.Request.ReadFormAsync(new FormOptions {ValueLengthLimit=4*TranscriptOperations.MaximumCharacters,MultipartBodyLengthLimit=6L*1024*1024},context.RequestAborted);
            await antiforgery.ValidateRequestAsync(context);
            if(form["text"].Count!=1 || !Guid.TryParse(form["token"],out var token))return Results.BadRequest("Invalid correction submission.");
            var result=await operations.CorrectAsync(round,id,form["text"].ToString(),token,context.RequestAborted);
            return Results.Redirect($"/interviews/{round}/transcripts/{result}");
        }
        catch(AntiforgeryValidationException){return Results.BadRequest("Correction rejected. Reopen the private transcript page before submitting.");}
        catch(Exception ex) when(ex is ValidationException or InvalidDataException or IOException)
        {
            return Results.Content($"<html><body><h1>Correction not confirmed</h1><p>{HtmlEncoder.Default.Encode(ex.Message)}</p><p>Use browser Back to recover your text and retry the same submission. Check the transcript list before starting another copy.</p><a href=\"/interviews/{round}\">Interview details</a></body></html>","text/html",statusCode:400);
        }
    }
}
