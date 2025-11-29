using System.Text.Json.Serialization;

namespace Auth.Api.Dtos;

public class UserLoginDto
{
    [JsonPropertyName("steamId")] public string SteamId { get; set; } = default!;

    [JsonPropertyName("personaName")] public string PersonaName { get; set; } = default!;

    [JsonPropertyName("profileUrl")] public string ProfileUrl { get; set; } = default!;
}