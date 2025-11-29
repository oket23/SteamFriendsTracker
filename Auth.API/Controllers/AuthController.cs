using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.Api.Dtos;
using Auth.Api.Models;
using Auth.Api.Options;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Enums;
using Shared.Models;

namespace Auth.Api.Controllers;

[ApiController]
[Route("/steam")]
public class SteamAuthController : ControllerBase
{
    private readonly AuthDbService _authDbService;
    private readonly AuthOptions _authOptions;
    private readonly AuthService _authService;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SteamAuthController> _logger;
    private readonly SteamApiService _steamApiService;

    public SteamAuthController(IDistributedCache cache, AuthDbService dbService, SteamApiService steamApiService,
        AuthService authService,
        IConfiguration configuration, ILogger<SteamAuthController> logger, IOptions<AuthOptions> authOptions)
    {
        _authOptions = authOptions.Value;
        _cache = cache;
        _authDbService = dbService;
        _authService = authService;
        _configuration = configuration;
        _steamApiService = steamApiService;
        _logger = logger;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        var url = _authService.BuildLoginUrl();
        return Redirect(url);
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback()
    {
        try
        {
            var steamId = await _authService.ValidateCallbackAndGetSteamIdAsync(Request.Query);
            if (steamId is null)
            {
                _logger.LogWarning("Steam OpenID validation failed");
                return BadRequest(new ApiErrorResponse
                {
                    IsSuccess = false,
                    Message = "Invalid Steam login.",
                    Error = "Steam OpenID validation failed."
                });
            }

            _logger.LogInformation("Steam callback validated for SteamId {SteamId}", steamId);

            var steamUser = await _steamApiService.GetUserAsync(steamId);
            if (steamUser is null)
            {
                _logger.LogWarning("Steam profile not found or failed to load for SteamId {SteamId}", steamId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiErrorResponse
                {
                    IsSuccess = false,
                    Message = "Unable to load Steam profile. Please try again later.",
                    Error = "SteamProfileUnavailable"
                });
            }

            var refresh = CreateRefreshToken();
            var dbUser = await _authDbService.GetUserById(steamId);

            int tokenVersion;

            if (dbUser == null)
            {
                var newUserDto = new DbUserDto
                {
                    Id = steamId,
                    RefreshToken = refresh.Token,
                    RefreshTokenExpiresAtUtc = refresh.ExpiresAtUtc,
                    CreatedAt = DateTime.UtcNow,
                    TokenVersion = 1
                };

                await _authDbService.CreateUserAsync(newUserDto);

                tokenVersion = newUserDto.TokenVersion;
            }
            else
            {
                dbUser.RefreshToken = refresh.Token;
                dbUser.RefreshTokenExpiresAtUtc = refresh.ExpiresAtUtc;
                dbUser.TokenVersion++;

                tokenVersion = dbUser.TokenVersion;

                await _authDbService.UpdateUserAsync(dbUser);
            }

            var response = new ApiSuccessResponse<AuthLoginDto>
            {
                IsSuccess = true,
                Message = "Login successful.",
                Data = new AuthLoginDto
                {
                    User = new UserLoginDto
                    {
                        SteamId = steamUser.SteamId,
                        PersonaName = steamUser.PersonaName,
                        ProfileUrl = steamUser.ProfileUrl
                    },
                    AccessToken = GenerateAccessToken(steamId, tokenVersion),
                    RefreshToken = refresh.Token
                }
            };

            await UpdateTokenVersionCacheAsync(steamId, tokenVersion);

            _logger.LogInformation("Steam login successful for SteamId {SteamId}", steamId);

            var redirectUrl = QueryHelpers.AddQueryString(
                _authOptions.FrontendCallbackUrl,
                new Dictionary<string, string?>
                {
                    ["accessToken"] = response.Data.AccessToken,
                    ["refreshToken"] = refresh.Token,
                    ["steamId"] = steamUser.SteamId,
                    ["personaName"] = steamUser.PersonaName,
                    ["profileUrl"] = steamUser.ProfileUrl
                });

            return Redirect(redirectUrl);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error during Steam callback");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                IsSuccess = false,
                Message = "An unexpected error occurred during Steam login.",
                Error = "InternalServerError"
            });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new ApiErrorResponse
                {
                    IsSuccess = false,
                    Message = "Refresh token is required.",
                    Error = "No refresh token provided."
                });

            var user = await _authDbService.GetUserByRefreshTokenAsync(request.RefreshToken);

            if (user is null || user.RefreshTokenExpiresAtUtc is null ||
                user.RefreshTokenExpiresAtUtc <= DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired refresh token for token {RefreshToken}", request.RefreshToken);

                return Unauthorized(new ApiErrorResponse
                {
                    IsSuccess = false,
                    Message = "Invalid or expired refresh token.",
                    Error = "InvalidRefreshToken"
                });
            }

            var newRefresh = CreateRefreshToken();
            user.RefreshToken = newRefresh.Token;
            user.RefreshTokenExpiresAtUtc = newRefresh.ExpiresAtUtc;
            user.TokenVersion++;

            await _authDbService.UpdateUserAsync(user);

            var accessToken = GenerateAccessToken(user.Id, user.TokenVersion);

            var response = new ApiSuccessResponse<AuthRefreshDto>
            {
                IsSuccess = true,
                Message = "Token refresh successful.",
                Data = new AuthRefreshDto
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefresh.Token
                }
            };

            await UpdateTokenVersionCacheAsync(user.Id, user.TokenVersion);

            _logger.LogInformation("Refresh token rotated successfully for user {UserId}", user.Id);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh for token {RefreshToken}",
                request.RefreshToken);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                IsSuccess = false,
                Message = "An unexpected error occurred during token refresh.",
                Error = "InternalServerError"
            });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        try
        {
            var steamIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub) ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (steamIdClaim is null)
            {
                _logger.LogWarning("Cannot get steamId from JWT for /auth/steam/me");
                return Unauthorized(new ApiErrorResponse
                {
                    IsSuccess = false,
                    Message = "Invalid token.",
                    Error = "Cannot extract userId from JWT."
                });
            }

            var steamId = steamIdClaim.Value;

            var user = await _authDbService.GetUserById(steamId);
            if (user is null)
            {
                _logger.LogWarning("User not found for id {UserId}", steamId);
                return NotFound(new ApiErrorResponse
                {
                    IsSuccess = false,
                    Message = "User not found.",
                    Error = "No user record in database."
                });
            }

            var steamUser = await _steamApiService.GetUserAsync(user.Id);
            if (steamUser is null)
            {
                _logger.LogWarning("Failed to load Steam profile for user {UserId}", user.Id);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiErrorResponse
                {
                    IsSuccess = false,
                    Message = "Unable to load Steam profile. Please try again later.",
                    Error = "SteamProfileUnavailable"
                });
            }

            var personaState = (SteamPersonaState)steamUser.PersonaState;

            var profile = new ProfileDto
            {
                SteamId = user.Id,
                PersonaName = steamUser.PersonaName,
                AvatarUrl = steamUser.AvatarFull,
                ProfileUrl = steamUser.ProfileUrl,
                CreatedAtUtc = user.CreatedAt,
                PersonaState = personaState,
                IsOnline = personaState switch
                {
                    SteamPersonaState.Online => true,
                    SteamPersonaState.Busy => true,
                    SteamPersonaState.Away => true,
                    SteamPersonaState.Snooze => true,
                    SteamPersonaState.LookingToPlay => true,
                    SteamPersonaState.LookingToTrade => true,
                    _ => false
                },
                CurrentGameId = steamUser.GameId,
                CurrentGameName = steamUser.GameExtraInfo,
                LastSeenAtUtc = steamUser.LastLogoff.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(steamUser.LastLogoff.Value).UtcDateTime
                    : null,
                SteamAccountCreatedAtUtc = steamUser.TimeCreated.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(steamUser.TimeCreated.Value).UtcDateTime
                    : null,
                CountryCode = steamUser.LocCountryCode
            };

            _logger.LogInformation("Profile fetched successfully for user {UserId}", user.Id);

            return Ok(new ApiSuccessResponse<ProfileDto>
            {
                IsSuccess = true,
                Message = "Profile fetched successfully.",
                Data = profile
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching profile for /auth/steam/me");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                IsSuccess = false,
                Message = "An unexpected error occurred while fetching profile.",
                Error = "InternalServerError"
            });
        }
    }


    private string GenerateAccessToken(string steamId, int tokenVersion)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var key = jwtSection["Key"];

        var lifetimeMinutes = int.TryParse(jwtSection["AccessTokenLifetimeMinutes"], out var minutes)
            ? minutes
            : 10;

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Jwt:Key is not configured");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(lifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, steamId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new("ver", tokenVersion.ToString())
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            now,
            expires,
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshTokenData CreateRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new RefreshTokenData
        {
            Token = token,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };
    }

    private Task UpdateTokenVersionCacheAsync(string userId, int tokenVersion)
    {
        var cacheKey = $"auth:token-version:{userId}";
        return _cache.SetStringAsync(cacheKey, tokenVersion.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        });
    }
}