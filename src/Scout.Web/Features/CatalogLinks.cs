namespace Scout.Web.Features;

public static class CatalogLinks
{
    // Carry only a query, never a caller-controlled destination or host.
    public static string List(string path, string? query) => path + (query?.StartsWith('?') == true ? query : string.Empty);
    public static string Record(string path, string? query) => path + (string.IsNullOrEmpty(query) ? string.Empty : "?listQuery=" + Uri.EscapeDataString(query));

    public static T? ParseEnum<T>(string? value) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var result) && Enum.IsDefined(result) ? result : null;
}
