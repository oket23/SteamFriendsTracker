using System.Text.Json.Serialization;

namespace Game.Api.Dtos;

public sealed class SteamStoreSearchResponse
{
    [JsonPropertyName("total")] public int Total { get; set; }

    [JsonPropertyName("items")] public List<SteamStoreSearchItem> Items { get; set; } = [];
}