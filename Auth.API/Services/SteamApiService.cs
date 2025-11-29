using System.Text.Json;
using Auth.Api.Models.SteamUser;
using Auth.Api.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Auth.Api.Services;

public class SteamApiService
{
    private readonly IDistributedCache _cache;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<SteamApiService> _logger;
    private readonly SteamApiOptions _options;

    public SteamApiService(HttpClient client, IDistributedCache cache, IOptions<SteamApiOptions> options,
        ILogger<SteamApiService> logger)
    {
        _client = client;
        _cache = cache;
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl)) _client.BaseAddress = new Uri(_options.BaseUrl);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<SteamUser> GetUserAsync(string steamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(steamId))
            throw new ArgumentException("SteamId cannot be null or empty.", nameof(steamId));

        var cacheKey = GetUserCacheKey(steamId);

        var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedJson))
            try
            {
                var fromCache = JsonSerializer.Deserialize<SteamUser>(cachedJson, _jsonOptions);
                if (fromCache != null)
                    return fromCache;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached SteamUser for key {CacheKey}", cacheKey);
            }

        try
        {
            var url = $"ISteamUser/GetPlayerSummaries/v2/?key={_options.ApiKey}&steamids={steamId}";

            var envelope = await _client.GetFromJsonAsync<SteamUserEnvelope>(url, cancellationToken);
            var user = envelope?.Response?.Players?.FirstOrDefault();

            if (user == null)
            {
                _logger.LogWarning("Steam returned no player for SteamId {SteamId}", steamId);
                return null;
            }

            var serialized = JsonSerializer.Serialize(user, _jsonOptions);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromSeconds(_options.UserCacheSeconds > 0 ? _options.UserCacheSeconds : 30)
            };

            await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calling Steam GetPlayerSummaries for SteamId {SteamId}", steamId);
            return null;
        }
    }

    private static string GetUserCacheKey(string steamId)
    {
        return $"steam:user:{steamId}";
    }
}