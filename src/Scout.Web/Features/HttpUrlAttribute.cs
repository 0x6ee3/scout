using System.ComponentModel.DataAnnotations;

namespace Scout.Web.Features;

public sealed class HttpUrlAttribute : ValidationAttribute
{
    public HttpUrlAttribute() : base("Enter a complete website address starting with http:// or https://.") { }

    public override bool IsValid(object? value) => value is null
        || value is string text && (string.IsNullOrWhiteSpace(text)
            || Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && !string.IsNullOrEmpty(uri.Host));
}
