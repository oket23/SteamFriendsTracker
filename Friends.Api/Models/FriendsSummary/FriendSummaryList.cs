using System.Text.Json.Serialization;

namespace Friends.Api.Models.FriendsSummary;

public class FriendSummaryList
{
    [JsonPropertyName("players")] public List<FriendSummary> Players { get; set; } = new();
}