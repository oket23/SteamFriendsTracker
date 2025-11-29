using System.Text.Json.Serialization;

namespace Auth.Api.Dtos;

public class AuthLoginDto
{
    [JsonPropertyName("user")]
    public UserLoginDto User { get; set; }
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; }
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; }
}
