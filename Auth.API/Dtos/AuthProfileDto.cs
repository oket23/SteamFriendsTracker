using System.Text.Json.Serialization;

namespace Auth.Api.Dtos;

public class AuthProfileDto
{
    [JsonPropertyName("user")]
    public ProfileDto User { get; set; }
}
