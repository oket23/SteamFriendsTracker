using System.Text.Json.Serialization;
using Shared.Enums;

namespace Friends.Api.Dtos;

public class FriendDetailsDto
{
    [JsonPropertyName("steamId")] public string SteamId { get; set; } = default!;

    [JsonPropertyName("personaName")] public string PersonaName { get; set; } = default!;

    [JsonPropertyName("avatarUrl")] public string AvatarUrl { get; set; } = default!;

    [JsonPropertyName("profileUrl")] public string ProfileUrl { get; set; } = default!;

    [JsonPropertyName("createdAtUtc")] public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("isOnline")] public bool IsOnline { get; set; }

    [JsonPropertyName("personaState")] public SteamPersonaState PersonaState { get; set; }

    [JsonPropertyName("currentGameId")] public string? CurrentGameId { get; set; }

    [JsonPropertyName("currentGameName")] public string? CurrentGameName { get; set; }

    [JsonPropertyName("lastSeenAtUtc")] public DateTime? LastSeenAtUtc { get; set; }

    [JsonPropertyName("steamAccountCreatedAtUtc")]
    public DateTime? SteamAccountCreatedAtUtc { get; set; }

    [JsonPropertyName("countryCode")] public string? CountryCode { get; set; }
}
