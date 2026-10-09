# MediaPager.Plugins.Search.Tmdb

Official **TMDB metadata + search provider** plugin for MediaPager. References the plugin SDK
(`MediaPager.App.PluginContracts`) only — no host, no web framework.

Plugin id: `mediapager.search.tmdb` · capabilities: `metadata`, `search`

## What it does

- `IMetadataProviderPlugin` — id-based detail sheets and enrichment for movies and TV:
  US content rating, genres, cast + key crew, seasons/episodes for TV, poster/backdrop.
- `ISearchProviderPlugin` — unified `/search/multi` type-ahead hits across movies and TV.
- `IPluginSettingsSchema` — data-driven settings read per call via `IPluginSettingsStore`.

## Settings (`plugins.tmdb.*`)

| Key | Type | Notes |
|---|---|---|
| `apiKey` | password, required, secret | TMDB API key (v3 auth). |
| `imageBase` | string, optional | Image base URL. Defaults to `https://image.tmdb.org/t/p/w500`. |

## Layout

- `TmdbProviderPlugin.cs` — the plugin class (descriptor + capability implementations).
- `Tmdb/` — ported TMDB client and DTOs (`TmdbClient`, `TmdbDtos`, `TmdbHelpers`).

## Building

```sh
dotnet build MediaPager.Plugins.Search.Tmdb/MediaPager.Plugins.Search.Tmdb.csproj
```
