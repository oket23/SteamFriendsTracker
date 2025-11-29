using System.Text.Json.Serialization;

namespace Shared.Models;

public class ApiSuccessResponse<T>
{
    [JsonPropertyName("success")]
    public bool IsSuccess { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; }
    [JsonPropertyName("data")]
    public T Data { get; set; }
}
