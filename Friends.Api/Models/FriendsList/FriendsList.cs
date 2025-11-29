using System.Text.Json.Serialization;

namespace Friends.Api.Models.FriendsList;

public class FriendsList
{
    [JsonPropertyName("friends")] public List<Friend> Friends { get; set; } = new();
}