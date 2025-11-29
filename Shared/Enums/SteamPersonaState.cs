using System.Text.Json.Serialization;

namespace Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SteamPersonaState
{
    Offline = 0,
    Online = 1,
    Busy = 2,
    Away = 3,
    Snooze = 4,
    LookingToTrade = 5,
    LookingToPlay = 6
}