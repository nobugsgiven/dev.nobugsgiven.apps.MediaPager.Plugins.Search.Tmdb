namespace MediaPager.Plugins.Search.Tmdb.Tmdb;

internal static class TmdbHelpers
{
    // Build a full image URL from a TMDB image path, passing through absolute URLs.
    public static string? ImageUrl(string imageBase, string? imagePath) => imagePath switch
    {
        null or "" => null,
        _ when imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase) => imagePath,
        _ => $"{imageBase}{(imagePath.StartsWith('/') ? imagePath : "/" + imagePath)}",
    };

    public static int? Year(string? date) =>
        date is { Length: >= 4 } d && int.TryParse(d[..4], out var year) ? year : null;

    public static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public static DateTimeOffset? ParseDate(string? value) =>
        value is { Length: >= 8 } && DateTimeOffset.TryParse(value, out var date) ? date.Date : null;
}