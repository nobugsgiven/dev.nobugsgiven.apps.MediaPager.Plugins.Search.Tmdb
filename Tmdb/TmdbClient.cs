using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace MediaPager.Plugins.Search.Tmdb.Tmdb;

/// <summary>
/// Thin JSON client over api.themoviedb.org v3. One long-lived HttpClient per plugin
/// instance; every call is best-effort and returns null instead of throwing so a
/// transient TMDB outage degrades gracefully on the consuming side.
/// </summary>
internal sealed class TmdbClient(HttpClient http)
{
    private const string BaseUrl = "https://api.themoviedb.org/3/";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<TmdbUnifiedHit>?> SearchMultiAsync(string apiKey, string query, CancellationToken ct)
    {
        var data = await GetAsync<TmdbUnifiedSearchResponse>(
            $"search/multi?api_key={Uri.EscapeDataString(apiKey)}&language=en-US&page=1&query={Uri.EscapeDataString(query)}", ct);
        return data?.Results;
    }

    public Task<TmdbMovieDetails?> GetMovieAsync(string apiKey, long id, CancellationToken ct) =>
        GetAsync<TmdbMovieDetails>($"movie/{id}?api_key={Uri.EscapeDataString(apiKey)}&language=en-US&append_to_response=credits,similar", ct);

    public Task<TmdbTvDetails?> GetTvAsync(string apiKey, long id, CancellationToken ct) =>
        GetAsync<TmdbTvDetails>($"tv/{id}?api_key={Uri.EscapeDataString(apiKey)}&language=en-US&append_to_response=credits,similar", ct);

    public Task<TmdbReleaseDatesResponse?> GetMovieReleaseDatesAsync(string apiKey, long id, CancellationToken ct) =>
        GetAsync<TmdbReleaseDatesResponse>($"movie/{id}/release_dates?api_key={Uri.EscapeDataString(apiKey)}", ct);

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct) where T : class
    {
        var url = BaseUrl + path;
        try
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"[tmdb] {path.Split('?')[0]} returned {(int)response.StatusCode} {response.ReasonPhrase}");
                return null;
            }

            return await response.Content.ReadFromJsonAsync<T>(Json, ct);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[tmdb] {path.Split('?')[0]} request failed ({exception.GetType().Name}): {exception.Message}");
            return null;
        }
    }
}