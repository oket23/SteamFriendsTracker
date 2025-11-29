using System.Text.Json;
using Game.Api.Dtos;
using Microsoft.Extensions.Caching.Distributed;

namespace Game.Api.Services;

public class SteamStoreApiService
{
    private readonly IDistributedCache _cache;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<SteamStoreApiService> _logger;

    public SteamStoreApiService(
        HttpClient client,
        ILogger<SteamStoreApiService> logger,
        IDistributedCache cache)
    {
        _client = client;
        _logger = logger;
        _cache = cache;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<SteamStoreSearchResponse?> SearchAsync(
        string term,
        string language = "english",
        string cc = "UA")
    {
        if (string.IsNullOrWhiteSpace(term)) return null;

        var normalized = term.Trim().ToLowerInvariant();
        var cacheKey = $"steam:storesearch:{cc}:{language}:{normalized}";

        var cached = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
            try
            {
                return JsonSerializer.Deserialize<SteamStoreSearchResponse>(cached, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached storesearch for key {CacheKey}", cacheKey);
            }

        try
        {
            var url =
                $"/api/storesearch/?term={Uri.EscapeDataString(term)}&l={Uri.EscapeDataString(language)}&cc={Uri.EscapeDataString(cc)}";

            var res = await _client.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Steam storesearch failed: {Status} {Url}", (int)res.StatusCode, url);
                return null;
            }

            var json = await res.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<SteamStoreSearchResponse>(json, _jsonOptions);
            if (data is null) return null;

            await _cache.SetStringAsync(
                cacheKey,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calling Steam storesearch for term {Term}", term);
            return null;
        }
    }
}