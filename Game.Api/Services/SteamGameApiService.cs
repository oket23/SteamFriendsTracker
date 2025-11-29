using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace Game.Api.Services;

public class SteamGameApiService
{
    private readonly IDistributedCache _cache;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<SteamGameApiService> _logger;

    public SteamGameApiService(
        HttpClient client,
        ILogger<SteamGameApiService> logger,
        IDistributedCache cache)
    {
        _client = client;
        _logger = logger;
        _cache = cache;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
    }

    public async Task<SteamAppDetails?> GetGameByIdAsync(string gameId, string language,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("GameId cannot be null or empty.", nameof(gameId));

        var cacheKey = GetGameCacheKey(gameId, language);

        var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedJson))
            try
            {
                var fromCache = JsonSerializer.Deserialize<SteamAppDetails>(cachedJson, _jsonOptions);
                if (fromCache is not null)
                {
                    _logger.LogDebug("Steam game {GameId} loaded from cache", gameId);
                    return fromCache;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to deserialize cached SteamAppDetails for key {CacheKey}", cacheKey);
            }

        try
        {
            var response = await _client.GetAsync($"/api/appdetails?appids={gameId}&l={language}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Steam Store API returned status {StatusCode} for app {GameId}",
                    response.StatusCode, gameId);

                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            Dictionary<string, AppDetailsEnvelope>? envelopeDict;
            try
            {
                envelopeDict = JsonSerializer.Deserialize<Dictionary<string, AppDetailsEnvelope>>(
                    responseContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to deserialize Steam appdetails response for game {GameId}. Body: {Body}",
                    gameId, responseContent);
                return null;
            }

            var envelope = envelopeDict?.Values.FirstOrDefault();
            if (envelope is null || !envelope.Success || envelope.Data is null)
            {
                _logger.LogWarning("Steam appdetails response invalid or 'success' == false for game {GameId}", gameId);
                return null;
            }

            var details = envelope.Data;

            try
            {
                var serialized = JsonSerializer.Serialize(details, _jsonOptions);

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                };

                await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to cache SteamAppDetails for game {GameId} with key {CacheKey}",
                    gameId, cacheKey);
            }

            return details;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error while calling Steam Store appdetails for game {GameId}", gameId);
            return null;
        }
    }

    private static string GetGameCacheKey(string gameId, string lang)
    {
        return $"steam:game:{gameId}:lang:{lang}";
    }
}