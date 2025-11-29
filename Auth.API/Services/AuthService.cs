using Auth.Api.Options;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace Auth.Api.Services;

public class AuthService
{
    private const string SteamOpenIdEndpoint = "https://steamcommunity.com/openid/login";

    private readonly ILogger<AuthService> _logger;
    private readonly HttpClient _client;
    private readonly AuthOptions _options;

    public AuthService(ILogger<AuthService> logger, HttpClient client, IOptions<AuthOptions> options)
    {
        _logger = logger;
        _client = client;
        _options = options.Value;
    }

    public string BuildLoginUrl()
    {
        var backendBase = _options.PublicBackendUrl.TrimEnd('/');

        var returnTo = $"{backendBase}/steam/callback";

        var realm = new Uri(backendBase).GetLeftPart(UriPartial.Authority);

        var queryBuilder = new QueryBuilder
        {
            { "openid.ns", "http://specs.openid.net/auth/2.0" },
            { "openid.mode", "checkid_setup" },
            { "openid.return_to", returnTo },
            { "openid.realm", realm },
            { "openid.claimed_id", "http://specs.openid.net/auth/2.0/identifier_select" },
            { "openid.identity", "http://specs.openid.net/auth/2.0/identifier_select" }
        };

        var loginUrl = SteamOpenIdEndpoint + queryBuilder.ToQueryString();
        _logger.LogInformation("Generated Steam login URL: {Url}", loginUrl);

        return loginUrl;
    }

    public async Task<string?> ValidateCallbackAndGetSteamIdAsync(IQueryCollection query)
    {
        var openIdParams = query
            .Where(kv => kv.Key.StartsWith("openid.", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

        if (!openIdParams.TryGetValue("openid.mode", out var mode) || mode != "id_res")
        {
            _logger.LogWarning("Invalid OpenID mode: {Mode}", mode);
            return null;
        }

        var validationPairs = new List<KeyValuePair<string, string>>();
        foreach (var kv in openIdParams)
        {
            if (kv.Key.Equals("openid.mode", StringComparison.OrdinalIgnoreCase))
                validationPairs.Add(new("openid.mode", "check_authentication"));
            else
                validationPairs.Add(new(kv.Key, kv.Value));
        }

        using var content = new FormUrlEncodedContent(validationPairs);
        using var response = await _client.PostAsync(SteamOpenIdEndpoint, content);
        var body = await response.Content.ReadAsStringAsync();

        if (!body.Contains("is_valid:true", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Steam OpenID validation failed. Response: {Body}", body);
            return null;
        }

        if (!openIdParams.TryGetValue("openid.claimed_id", out var claimedId))
        {
            _logger.LogWarning("openid.claimed_id is missing in callback");
            return null;
        }

        var segments = claimedId.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var steamId = segments.LastOrDefault();

        if (string.IsNullOrWhiteSpace(steamId))
        {
            _logger.LogWarning("Failed to parse SteamID from claimed_id: {ClaimedId}", claimedId);
            return null;
        }

        _logger.LogInformation("Successfully validated Steam OpenID. SteamID: {SteamId}", steamId);
        return steamId;
    }
}
