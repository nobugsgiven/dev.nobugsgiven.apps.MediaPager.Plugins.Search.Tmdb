using MediaPager.App.PluginContracts;
using MediaPager.Plugins.Search.Tmdb.Tmdb;

namespace MediaPager.Plugins.Search.Tmdb;

/// <summary>
/// Official TMDB provider: unified search (movies + TV), id-based metadata details, and
/// catalog enrichment. Settings live under plugins.tmdb.* and flow through the
/// SDK's IPluginSettingsStore; config is the fallback. Settings are read per call so a
/// runtime update takes effect on the next request without a restart.
/// </summary>
public sealed class TmdbProviderPlugin(IPluginSettingsStore settingsStore) :
    IMediaPagerPlugin, IPluginSettingsSchema, ISearchProviderPlugin, IMetadataProviderPlugin
{
    public const string PluginKey = "tmdb";
    public const string ApiKeySetting = "apiKey";
    public const string ImageBaseSetting = "imageBase";

    public const string DefaultImageBase = "https://image.tmdb.org/t/p/w500";
    public const string SourceKey = "mediapager.search.tmdb";
    public const string PluginId = "mediapager.metadata.tmdb";

    public PluginDescriptor Descriptor { get; } = new(
        Id: PluginId,
        Name: "TMDB",
        Version: "0.1.0",
        Author: "Nobugsgiven",
        Description: "The Movie Database metadata and search provider for movies and TV shows.");

    public IReadOnlyList<PluginSettingDefinition> Settings { get; } =
    [
        new PluginSettingDefinition(
            ApiKeySetting,
            "TMDB API key (v3 auth)",
            PluginSettingType.Password,
            Required: true,
            Secret: true),
        new PluginSettingDefinition(
            ImageBaseSetting,
            "TMDB image base URL",
            PluginSettingType.String,
            Default: DefaultImageBase),
    ];

    private readonly TmdbClient _client = new(new HttpClient { Timeout = TimeSpan.FromSeconds(30) });

    public bool Handles(MediaKind kind) => kind is MediaKind.Movie or MediaKind.Tv;

    public async Task<IReadOnlyList<ProviderSearchHit>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var query = request.Query?.Trim();
        if (string.IsNullOrWhiteSpace(query))
            return [];
        if (request.SupportedStreamKinds is { Count: 0 })
            return [];

        var apiKey = await ApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return [];

        var hits = await _client.SearchMultiAsync(apiKey, query, cancellationToken);
        if (hits is null || hits.Count == 0)
            return [];

        var imageBase = await ImageBaseAsync(cancellationToken);
        var limit = Math.Max(1, request.Limit);
        var supportedKinds = request.SupportedStreamKinds?.ToHashSet();

        return hits
            .Where(hit => hit.MediaType is "movie" or "tv")
            .Where(hit => supportedKinds is null || supportedKinds.Contains(
                hit.MediaType == "tv" ? MediaKind.Tv : MediaKind.Movie))
            .OrderBy(hit => hit.MediaType == "movie" ? 0 : 1)
            .Take(limit)
            .Select(hit => new ProviderSearchHit(
                Kind: hit.MediaType == "tv" ? MediaKind.Tv : MediaKind.Movie,
                ExternalId: hit.Id.ToString(),
                Title: hit.Name ?? hit.Title ?? "-",
                SourceKey: SourceKey,
                Year: TmdbHelpers.Year(hit.FirstAirDate ?? hit.ReleaseDate),
                VoteAverage: hit.VoteAverage,
                Overview: hit.Overview ?? "",
                ArtworkUrl: TmdbHelpers.ImageUrl(imageBase, hit.PosterPath)))
            .ToList();
    }

    public async Task<TitleDetails?> GetDetailsAsync(MediaKind kind, string externalId, CancellationToken cancellationToken)
    {
        if (!Handles(kind) || !TryParseId(externalId, out var id))
            return null;

        var apiKey = await ApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var imageBase = await ImageBaseAsync(cancellationToken);

        if (kind == MediaKind.Movie)
        {
            var movie = await _client.GetMovieAsync(apiKey, id, cancellationToken);
            if (movie is null)
                return null;

            return new TitleDetails(
                ExternalId: movie.Id.ToString(),
                Kind: MediaKind.Movie,
                Title: movie.Title ?? movie.OriginalTitle ?? "-",
                Overview: TmdbHelpers.Clean(movie.Overview),
                Year: TmdbHelpers.Year(movie.ReleaseDate),
                OriginalTitle: TmdbHelpers.Clean(movie.OriginalTitle),
                Rating: Rating(movie.VoteAverage),
                OriginalAvailableAt: TmdbHelpers.ParseDate(movie.ReleaseDate),
                ArtworkUrl: TmdbHelpers.ImageUrl(imageBase, movie.PosterPath),
                BackdropUrl: TmdbHelpers.ImageUrl(imageBase, movie.BackdropPath),
                Credits: BuildCredits(movie.Credits, imageBase));
        }

        var tv = await _client.GetTvAsync(apiKey, id, cancellationToken);
        if (tv is null)
            return null;

        return new TitleDetails(
            ExternalId: tv.Id.ToString(),
            Kind: MediaKind.Tv,
            Title: tv.Name ?? tv.OriginalName ?? "-",
            Overview: TmdbHelpers.Clean(tv.Overview),
            Year: TmdbHelpers.Year(tv.FirstAirDate),
            OriginalTitle: TmdbHelpers.Clean(tv.OriginalName),
            Rating: Rating(tv.VoteAverage),
            OriginalAvailableAt: TmdbHelpers.ParseDate(tv.FirstAirDate),
            ArtworkUrl: TmdbHelpers.ImageUrl(imageBase, tv.PosterPath),
            BackdropUrl: TmdbHelpers.ImageUrl(imageBase, tv.BackdropPath),
            Credits: BuildCredits(tv.Credits, imageBase),
            Seasons: BuildSeasons(tv.Seasons, imageBase));
    }

    public async Task<EnrichmentData?> EnrichAsync(string externalId, MediaKind kind, CancellationToken cancellationToken)
    {
        if (!Handles(kind) || !TryParseId(externalId, out var id))
            return null;

        var apiKey = await ApiKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var imageBase = await ImageBaseAsync(cancellationToken);

        if (kind == MediaKind.Movie)
        {
            var movie = await _client.GetMovieAsync(apiKey, id, cancellationToken);
            if (movie is null)
                return null;

            return new EnrichmentData(
                Title: movie.Title ?? movie.OriginalTitle ?? "-",
                Overview: TmdbHelpers.Clean(movie.Overview),
                Year: TmdbHelpers.Year(movie.ReleaseDate),
                OriginalTitle: TmdbHelpers.Clean(movie.OriginalTitle),
                ContentRating: await UsContentRatingAsync(apiKey, id, cancellationToken),
                Rating: Rating(movie.VoteAverage),
                OriginalAvailableAt: TmdbHelpers.ParseDate(movie.ReleaseDate),
                ArtworkUrl: TmdbHelpers.ImageUrl(imageBase, movie.PosterPath),
                BackdropUrl: TmdbHelpers.ImageUrl(imageBase, movie.BackdropPath),
                Genres: BuildGenres(movie.Genres),
                Credits: BuildCredits(movie.Credits, imageBase));
        }

        var tv = await _client.GetTvAsync(apiKey, id, cancellationToken);
        if (tv is null)
            return null;

        return new EnrichmentData(
            Title: tv.Name ?? tv.OriginalName ?? "-",
            Overview: TmdbHelpers.Clean(tv.Overview),
            Year: TmdbHelpers.Year(tv.FirstAirDate),
            OriginalTitle: TmdbHelpers.Clean(tv.OriginalName),
            Rating: Rating(tv.VoteAverage),
            OriginalAvailableAt: TmdbHelpers.ParseDate(tv.FirstAirDate),
            ArtworkUrl: TmdbHelpers.ImageUrl(imageBase, tv.PosterPath),
            BackdropUrl: TmdbHelpers.ImageUrl(imageBase, tv.BackdropPath),
            Genres: BuildGenres(tv.Genres),
            Credits: BuildCredits(tv.Credits, imageBase));
    }

    // US certification is movie-only today (TMDB exposes it via /movie/{id}/release_dates);
    // TV certifications have no equivalent in v3 without extra lookups.
    private async Task<string?> UsContentRatingAsync(string apiKey, long movieId, CancellationToken cancellationToken)
    {
        var releases = await _client.GetMovieReleaseDatesAsync(apiKey, movieId, cancellationToken);
        var usReleases = releases?.Results
            ?.FirstOrDefault(region => string.Equals(region.Iso, "US", StringComparison.OrdinalIgnoreCase))
            ?.ReleaseDates;
        if (usReleases is null || usReleases.Count == 0)
            return null;

        return usReleases
            .Where(release => !string.IsNullOrWhiteSpace(release.Certification))
            .Select(release => release.Certification!.Trim())
            .FirstOrDefault();
    }

    private IReadOnlyList<CreditRole>? BuildCredits(TmdbCredits? credits, string imageBase)
    {
        if (credits is null)
            return null;

        var list = new List<CreditRole>();
        foreach (var castMember in (credits.Cast ?? []).OrderBy(member => member.Order).Take(12))
            list.Add(new CreditRole(castMember.Name ?? "-", castMember.Character ?? "", TmdbHelpers.ImageUrl(imageBase, castMember.ProfilePath)));

        foreach (var crewMember in (credits.Crew ?? []).Where(member => IsKeyCrew(member.Job)))
            list.Add(new CreditRole(crewMember.Name ?? "-", crewMember.Job ?? "Crew", null));

        return list.Count > 0 ? list : null;
    }

    private static bool IsKeyCrew(string? job) => job is not null && (
        string.Equals(job, "Director", StringComparison.OrdinalIgnoreCase) ||
        job.Contains("Writer", StringComparison.OrdinalIgnoreCase) ||
        job.Contains("Screenplay", StringComparison.OrdinalIgnoreCase) ||
        job.Contains("Story", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(job, "Producer", StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<SeasonInfo>? BuildSeasons(List<TmdbSeason>? seasons, string imageBase)
    {
        if (seasons is null || seasons.Count == 0)
            return null;

        return seasons
            .Where(season => season.SeasonNumber > 0 && season.EpisodeCount > 0)
            .OrderBy(season => season.SeasonNumber)
            .Select(season => new SeasonInfo(
                SeasonNumber: season.SeasonNumber,
                Name: TmdbHelpers.Clean(season.Name),
                Overview: TmdbHelpers.Clean(season.Overview),
                PosterUrl: TmdbHelpers.ImageUrl(imageBase, season.PosterPath)))
            .ToList();
    }

    private static IReadOnlyList<string>? BuildGenres(List<TmdbGenre>? genres)
    {
        var names = (genres ?? []).Select(genre => TmdbHelpers.Clean(genre.Name)).OfType<string>().ToList();
        return names.Count > 0 ? names : null;
    }

    private static double? Rating(double voteAverage) =>
        voteAverage > 0 ? Math.Round(voteAverage, 1) : null;

    private static bool TryParseId(string externalId, out long id) =>
        long.TryParse(externalId, out id) && id > 0;

    private async Task<string?> ApiKeyAsync(CancellationToken cancellationToken) =>
        (await settingsStore.GetAsync(PluginKey, ApiKeySetting, cancellationToken))?.Trim();

    private async Task<string> ImageBaseAsync(CancellationToken cancellationToken) =>
        (await settingsStore.GetAsync(PluginKey, ImageBaseSetting, cancellationToken)) is { Length: > 0 } value
            ? value.TrimEnd('/')
            : DefaultImageBase;
}
