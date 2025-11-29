using System.Text.Json.Serialization;

namespace Auth.Api.Models.SteamUser;

public class SteamUserResponse
{
    [JsonPropertyName("players")]
    public List<SteamUser> Players { get; set; }
}
