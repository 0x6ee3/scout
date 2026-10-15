using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Scout.Web.Data;

namespace Scout.Web.Features;

public static partial class Tags
{
    [GeneratedRegex(@"^[\p{L}\p{N}][\p{L}\p{M}\p{N}]*(?:-[\p{L}\p{N}][\p{L}\p{M}\p{N}]*)*$")]
    private static partial Regex Token();

    public static List<string> Normalize(IEnumerable<string> values)
    {
        var tags = values.Select(x => (x ?? "").Trim().Normalize().ToLowerInvariant()).Distinct().ToList();
        if (tags.Count > 20 || tags.Any(x => x.Length is 0 or > 50 || !Token().IsMatch(x)))
            throw new ValidationException("Use up to 20 tags of 1–50 characters: words or hyphen-separated words, such as team-conflict.");
        return tags.Order(StringComparer.Ordinal).ToList();
    }

    public static string Color(string tag)
    {
        uint hash = 2166136261;
        foreach (var c in tag) hash = unchecked((hash ^ c) * 16777619);
        return $"tag-color-{hash % 6}";
    }
}

public sealed class TagOperations(IDbContextFactory<ScoutDbContext> factory)
{
    public async Task<IReadOnlyList<string>> SuggestionsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.EmployerQuestionTags.Select(x => x.Tag).Union(db.ExperienceTags.Select(x => x.Tag)).OrderBy(x => x).ToListAsync();
    }
}
