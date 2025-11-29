namespace Auth.Api.Options;

public class SteamApiOptions
{
    public string ApiKey { get; set; } = default!;
    public string BaseUrl { get; set; } = "https://api.steampowered.com";
    public int UserCacheSeconds { get; set; } = 60;
}
