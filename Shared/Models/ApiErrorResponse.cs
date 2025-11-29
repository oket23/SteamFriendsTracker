using System.Text.Json.Serialization;

namespace Shared.Models;

public class ApiErrorResponse
{
    [JsonPropertyName("success")]
    public bool IsSuccess { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; }
    [JsonPropertyName("error")]
    public string Error { get; set; }
}
