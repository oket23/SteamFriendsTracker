using System.Text.Json.Serialization;

namespace Auth.Api.Models.SteamUser;

public class SteamUserEnvelope
{
    [JsonPropertyName("response")]
    public SteamUserResponse Response { get; set; } = default!;
}
