using System.Text.Json;
using Friends.Api.Dtos;
using Friends.Api.Models.FriendsList;
using Friends.Api.Models.FriendsSummary;
using Friends.Api.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Shared.Enums;

namespace Friends.Api.Services;

public class SteamApiClient
{
    private readonly IDistributedCache _cache;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<SteamApiClient> _logger;
    private readonly SteamApiOptions _options;

    public SteamApiClient(
        HttpClient client,
        IOptions<SteamApiOptions> options,
        ILogger<SteamApiClient> logger, IDistributedCache cache)
    {
        _cache = cache;
        _client = client;
        _options = options.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private async Task<FriendsList> GetFriendIdsAsync(string ownerSteamId, CancellationToken ct)
    {
        var url = $"/ISteamUser/GetFriendList/v1/?key={_options.ApiKey}&steamid={ownerSteamId}&relationship=friend";

        var response = await _client.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning($"GetFriendList returned {response.StatusCode} for user {ownerSteamId}");
            return new FriendsList();
        }

        var json = await response.Content.ReadAsStringAsync(ct);

        try
        {
            var data = JsonSerializer.Deserialize<FriendsResponse>(json, _jsonOptions);

            if (data?.FriendsList?.Friends == null)
            {
                _logger.LogWarning(
                    $"GetFriendList response is null or missing friendslist for user {ownerSteamId}. Body: {json}");
                return new FriendsList();
            }

            return data.FriendsList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to deserialize GetFriendList for user {ownerSteamId}. Body: {json}");
            return new FriendsList();
        }
    }

    private async Task<List<FriendsSummaryDto>> GetPlayerSummariesAsync(IEnumerable<Friend> friends, CancellationToken ct)
    {
        var ids = friends
            .Select(f => f.SteamId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (!ids.Any()) return new List<FriendsSummaryDto>();

        var idsJoined = string.Join(",", ids);
        var url = $"/ISteamUser/GetPlayerSummaries/v2/?key={_options.ApiKey}&steamids={idsJoined}";

        var response = await _client.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning($"GetPlayerSummaries returned {response.StatusCode}");
            return new List<FriendsSummaryDto>();
        }

        var content = await response.Content.ReadAsStringAsync(ct);

        try
        {
            var apiResponse = JsonSerializer.Deserialize<FriendSummaryResponse>(content, _jsonOptions);
            var players = apiResponse?.Response?.Players;

            if (players == null || players.Count == 0)
            {
                _logger.LogWarning($"GetPlayerSummaries response is null or contains no players. Body: {content}");
                return new List<FriendsSummaryDto>();
            }

            return players
                .Select(p => MapFriends(p))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to deserialize GetPlayerSummaries response. Body: {content}");
            return new List<FriendsSummaryDto>();
        }
    }

    public async Task<List<FriendsSummaryDto>> GetFriendsStatusAsync(string ownerSteamId, CancellationToken ct = default)
    {
        var friendIds = await GetFriendIdsAsync(ownerSteamId, ct);
        if (friendIds.Friends.Count == 0) return new List<FriendsSummaryDto>();

        var result = new List<FriendsSummaryDto>();

        foreach (var batch in friendIds.Friends.Chunk(100))
        {
            var batchStatuses = await GetPlayerSummariesAsync(batch, ct);
            result.AddRange(batchStatuses);

            await Task.Delay(150, ct);
        }

        return result;
    }

    public async Task<FriendDetailsDto?> GetFriendSummaryById(string steamId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(steamId))
        {
            throw new ArgumentException("SteamId cannot be null or empty.", nameof(steamId));
        }

        var cacheKey = GetFriendCacheKey(steamId);

        var cachedJson = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cachedJson))
            try
            {
                var fromCache = JsonSerializer.Deserialize<FriendDetailsDto>(cachedJson, _jsonOptions);
                if (fromCache != null)
                {
                    _logger.LogDebug($"Friend summary for {steamId} loaded from cache with key {cacheKey}");
                    return fromCache;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to deserialize cached FriendsSummaryDto for key {cacheKey}");
            }

        try
        {
            var url = $"/ISteamUser/GetPlayerSummaries/v2/?key={_options.ApiKey}&steamids={steamId}";

            var response = await _client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"GetPlayerSummaries returned {response.StatusCode} for SteamId {steamId}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);

            var apiResponse = JsonSerializer.Deserialize<FriendSummaryResponse>(content, _jsonOptions);

            var player = apiResponse?.Response?.Players?.FirstOrDefault();
            if (player is null)
            {
                _logger.LogWarning($"Steam returned no player for SteamId {steamId}");
                return null;
            }

            var dto = new FriendDetailsDto()
            {
                SteamId = player.SteamId,
                PersonaName = player.PersonaName,
                AvatarUrl = player.AvatarFull,
                ProfileUrl = player.ProfileUrl,
                PersonaState = (SteamPersonaState)player.PersonaState,
                IsOnline = (SteamPersonaState)player.PersonaState switch
                {
                    SteamPersonaState.Online => true,
                    SteamPersonaState.Busy => true,
                    SteamPersonaState.Away => true,
                    SteamPersonaState.Snooze => true,
                    SteamPersonaState.LookingToPlay => true,
                    SteamPersonaState.LookingToTrade => true,
                    _ => false
                },
                CurrentGameId = player.GameId,
                CurrentGameName = player.GameExtraInfo,
                LastSeenAtUtc = player.LastLogoff.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(player.LastLogoff.Value).UtcDateTime
                    : null,
                SteamAccountCreatedAtUtc = player.TimeCreated.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(player.TimeCreated.Value).UtcDateTime
                    : null,
                CountryCode = player.LocCountryCode
            };

            try
            {
                var serialized = JsonSerializer.Serialize(dto, _jsonOptions);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromSeconds(_options.UserCacheSeconds > 0 ? _options.UserCacheSeconds : 30)
                };
                await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to cache FriendsSummaryDto for SteamId {steamId} with key {cacheKey}");
            }

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while calling Steam GetPlayerSummaries for SteamId {steamId}");
            return null;
        }
    }

    private static string GetFriendCacheKey(string steamId)
    {
        return $"steam:friend:{steamId}";
    }

    private static FriendsSummaryDto MapFriends(FriendSummary friend)
    {
        return new FriendsSummaryDto
        {
            SteamId = friend.SteamId,
            PersonaName = friend.PersonaName,
            AvatarUrl = friend.AvatarFull,
            ProfileUrl = friend.ProfileUrl,
            PersonaState = (SteamPersonaState)friend.PersonaState,
            IsOnline = (SteamPersonaState)friend.PersonaState switch
            {
                SteamPersonaState.Online => true,
                SteamPersonaState.Busy => true,
                SteamPersonaState.Away => true,
                SteamPersonaState.Snooze => true,
                SteamPersonaState.LookingToPlay => true,
                SteamPersonaState.LookingToTrade => true,
                _ => false
            },
            CurrentGameId = friend.GameId,
            CurrentGameName = friend.GameExtraInfo,
            LastSeenAtUtc = friend.LastLogoff.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(friend.LastLogoff.Value).UtcDateTime
                : null
        };
    }
}