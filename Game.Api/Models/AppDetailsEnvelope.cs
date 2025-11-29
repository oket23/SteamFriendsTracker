using System.Text.Json.Serialization;

public class AppDetailsEnvelope
{
    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonPropertyName("data")] public SteamAppDetails Data { get; set; } = default!;
}