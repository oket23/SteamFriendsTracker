using System.Text.Json.Serialization;

namespace Auth.Api.Dtos;

public class AuthRefreshDto
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; }
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; }
}
