using System.Text.Json.Serialization;

namespace Auth.Api.Models.SteamUser;

public class SteamUser
{
    [JsonPropertyName("steamid")]
    public string SteamId { get; set; } = default!;

    [JsonPropertyName("communityvisibilitystate")]
    public int CommunityVisibilityState { get; set; }

    [JsonPropertyName("profilestate")]
    public int ProfileState { get; set; }

    [JsonPropertyName("personaname")]
    public string PersonaName { get; set; } = default!;

    [JsonPropertyName("lastlogoff")]
    public long? LastLogoff { get; set; }

    [JsonPropertyName("profileurl")]
    public string ProfileUrl { get; set; } = default!;

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; } = default!;

    [JsonPropertyName("avatarmedium")]
    public string AvatarMedium { get; set; } = default!;

    [JsonPropertyName("avatarfull")]
    public string AvatarFull { get; set; } = default!;

    [JsonPropertyName("personastate")]
    public int PersonaState { get; set; }

    // Optional fields

    [JsonPropertyName("commentpermission")]
    public int? CommentPermission { get; set; }

    [JsonPropertyName("realname")]
    public string? RealName { get; set; }

    [JsonPropertyName("primaryclanid")]
    public string? PrimaryClanId { get; set; }

    [JsonPropertyName("timecreated")]
    public long? TimeCreated { get; set; }

    [JsonPropertyName("loccountrycode")]
    public string? LocCountryCode { get; set; }

    [JsonPropertyName("locstatecode")]
    public string? LocStateCode { get; set; }

    [JsonPropertyName("loccityid")]
    public int? LocCityId { get; set; }

    [JsonPropertyName("gameid")]
    public string? GameId { get; set; }

    [JsonPropertyName("gameextrainfo")]
    public string? GameExtraInfo { get; set; }

    [JsonPropertyName("gameserverip")]
    public string? GameServerIp { get; set; }

    // Поле, яке є в живому респонсі, хоч і не в старій документації
    [JsonPropertyName("personastateflags")]
    public int? PersonaStateFlags { get; set; }
}
