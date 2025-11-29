using System.Text.Json.Serialization;

namespace Friends.Api.Models.FriendsSummary;

public class FriendSummaryResponse
{
    [JsonPropertyName("response")] public FriendSummaryList Response { get; set; } = null!;
}