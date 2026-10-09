using System.Text.Json.Serialization;

namespace MediaPager.Plugins.Search.Tmdb.Tmdb;

// TMDB upstream response shapes (deserialization targets for api.themoviedb.org).
// Ported from the legacy Api's TmdbDtos; only shapes the plugin actually reads remain.

public sealed record TmdbResponse(
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("results")] List<UpstreamMovie> Results,
    [property: JsonPropertyName("total_pages")] int TotalPages,
    [property: JsonPropertyName("total_results")] int TotalResults);

public sealed record UpstreamMovie(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("first_air_date")] string? FirstAirDate,
    [property: JsonPropertyName("vote_average")] double VoteAverage,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("backdrop_path")] string? BackdropPath);

public sealed record TmdbMovieDetails(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("original_title")] string? OriginalTitle,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("vote_average")] double VoteAverage,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("backdrop_path")] string? BackdropPath,
    [property: JsonPropertyName("runtime")] int? Runtime,
    [property: JsonPropertyName("genres")] List<TmdbGenre>? Genres,
    [property: JsonPropertyName("production_countries")] List<TmdbCountry>? ProductionCountries,
    [property: JsonPropertyName("credits")] TmdbCredits? Credits,
    [property: JsonPropertyName("similar")] TmdbResponse? Similar);

public sealed record TmdbTvDetails(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("original_name")] string? OriginalName,
    [property: JsonPropertyName("first_air_date")] string? FirstAirDate,
    [property: JsonPropertyName("last_air_date")] string? LastAirDate,
    [property: JsonPropertyName("vote_average")] double VoteAverage,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("backdrop_path")] string? BackdropPath,
    [property: JsonPropertyName("number_of_seasons")] int? NumberOfSeasons,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("genres")] List<TmdbGenre>? Genres,
    [property: JsonPropertyName("created_by")] List<TmdbCreator>? CreatedBy,
    [property: JsonPropertyName("credits")] TmdbCredits? Credits,
    [property: JsonPropertyName("similar")] TmdbResponse? Similar,
    [property: JsonPropertyName("seasons")] List<TmdbSeason>? Seasons);

public sealed record TmdbGenre([property: JsonPropertyName("name")] string? Name);

public sealed record TmdbCountry([property: JsonPropertyName("name")] string? Name);

public sealed record TmdbCreator([property: JsonPropertyName("name")] string? Name);

public sealed record TmdbSeason(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("season_number")] int SeasonNumber,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("air_date")] string? AirDate,
    [property: JsonPropertyName("episode_count")] int EpisodeCount,
    [property: JsonPropertyName("poster_path")] string? PosterPath);

public sealed record TmdbCredits(
    [property: JsonPropertyName("cast")] List<TmdbCast>? Cast,
    [property: JsonPropertyName("crew")] List<TmdbCrew>? Crew);

public sealed record TmdbCast(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("character")] string? Character,
    [property: JsonPropertyName("profile_path")] string? ProfilePath,
    [property: JsonPropertyName("order")] int Order);

public sealed record TmdbCrew(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("job")] string? Job);

public sealed record TmdbReleaseDatesResponse(
    [property: JsonPropertyName("results")] List<TmdbReleaseDatesForRegion>? Results);

public sealed record TmdbReleaseDatesForRegion(
    [property: JsonPropertyName("iso_3166_1")] string? Iso,
    [property: JsonPropertyName("release_dates")] List<TmdbReleaseDate>? ReleaseDates);

public sealed record TmdbReleaseDate([property: JsonPropertyName("certification")] string? Certification);

// Unified search: search/multi returns movies, TV shows, and people in one call.
public sealed record TmdbUnifiedSearchResponse(
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("results")] List<TmdbUnifiedHit> Results,
    [property: JsonPropertyName("total_pages")] int TotalPages,
    [property: JsonPropertyName("total_results")] int TotalResults);

public sealed record TmdbUnifiedHit(
    [property: JsonPropertyName("media_type")] string? MediaType,
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("first_air_date")] string? FirstAirDate,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("vote_average")] double VoteAverage);