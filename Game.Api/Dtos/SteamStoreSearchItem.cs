using System.Text.Json.Serialization;

namespace Game.Api.Dtos;

public sealed class SteamStoreSearchItem
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; } = "";

    [JsonPropertyName("tiny_image")] public string TinyImage { get; set; } = "";

    [JsonPropertyName("price")] public SteamPrice? Price { get; set; }
}