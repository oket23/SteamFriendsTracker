using System.Text.Json;
using Friends.Api.Dtos;
using Microsoft.Extensions.Caching.Distributed;

namespace Friends.Api.Services;

public class FriendsSnapshotService
{
    private readonly ILogger<FriendsSnapshotService> _logger;
    private readonly SteamApiClient _steamApiClient;
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly TimeSpan SnapshotTtl = TimeSpan.FromSeconds(30);

    public FriendsSnapshotService(
        SteamApiClient steamApiClient,
        ILogger<FriendsSnapshotService> logger,
        IDistributedCache cache)
    {
        _steamApiClient = steamApiClient;
        _logger = logger;
        _cache = cache;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<FriendsSnapshotDto> GetSnapshotAsync(string ownerSteamId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ownerSteamId))
        {
            throw new ArgumentException("Owner steam id cannot be null or empty.", nameof(ownerSteamId));
        }

        var cacheKey = GetSnapshotCacheKey(ownerSteamId);
        
        var cachedJson = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            try
            {
                var fromCache = JsonSerializer.Deserialize<FriendsSnapshotDto>(cachedJson, _jsonOptions);
                if (fromCache is not null)
                {
                    _logger.LogDebug($"Friends snapshot for {ownerSteamId} loaded from cache with key {cacheKey}");
                    return fromCache;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    $"Failed to deserialize FriendsSnapshotDto from cache for user {ownerSteamId} with key {cacheKey}");
            }
        }
        
        _logger.LogInformation($"Loading friends snapshot from Steam for user {ownerSteamId}");

        var friends = await _steamApiClient.GetFriendsStatusAsync(ownerSteamId, ct);

        var snapshot = new FriendsSnapshotDto
        {
            OwnerSteamId = ownerSteamId,
            FetchedAtUtc = DateTime.UtcNow,
            Friends = friends
        };
        
        try
        {
            var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = SnapshotTtl
            };

            await _cache.SetStringAsync(cacheKey, json, options, ct);
            _logger.LogDebug($"Friends snapshot for {ownerSteamId} cached successfully with key {cacheKey} for {SnapshotTtl.TotalSeconds} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to cache FriendsSnapshotDto for user {ownerSteamId} with key {cacheKey}");
        }

        return snapshot;
    }

    private static string GetSnapshotCacheKey(string ownerSteamId)
    {
        return $"friends:snapshot:{ownerSteamId}";
    }
}
