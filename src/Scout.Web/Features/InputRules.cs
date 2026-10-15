using System.ComponentModel.DataAnnotations;

namespace Scout.Web.Features;

internal static class InputRules
{
    public static string? Trim(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    public static string? Text(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;
    public static T Validate<T>(T value) where T : class
    {
        Validator.ValidateObject(value, new ValidationContext(value), true);
        return value;
    }
}
