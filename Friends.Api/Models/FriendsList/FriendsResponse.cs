using System.Text.Json.Serialization;

namespace Friends.Api.Models.FriendsList;

public class FriendsResponse
{
    [JsonPropertyName("friendslist")] public FriendsList FriendsList { get; set; } = null!;
}