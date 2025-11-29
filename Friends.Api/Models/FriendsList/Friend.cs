using System.Text.Json.Serialization;

namespace Friends.Api.Models.FriendsList;

public class Friend
{
    [JsonPropertyName("steamid")] public string SteamId { get; set; } = null!;

    [JsonPropertyName("relationship")] public string Relationship { get; set; } = null!;

    [JsonPropertyName("friend_since")] public long FriendSince { get; set; }
}