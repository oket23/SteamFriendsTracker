using System.Text.Json.Serialization;

namespace Game.Api.Dtos;

public sealed class SteamPrice
{
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";

    [JsonPropertyName("initial")] public int Initial { get; set; }

    [JsonPropertyName("final")] public int Final { get; set; }
}