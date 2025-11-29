using System.Text.Json.Serialization;

namespace Friends.Api.Dtos;

public class FriendsSnapshotDto
{
    [JsonPropertyName("ownerSteamId")] public string OwnerSteamId { get; set; } = null!;

    [JsonPropertyName("fetchedAtUtc")] public DateTime FetchedAtUtc { get; set; }

    [JsonPropertyName("friends")] public List<FriendsSummaryDto> Friends { get; set; } = new();
}