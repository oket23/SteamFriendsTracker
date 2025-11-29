using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Models;

namespace Gateway.Api.Authentication;

public class JwtBearerEventHandlers : JwtBearerEvents
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<JwtBearerEventHandlers> _logger;

    public JwtBearerEventHandlers(IDistributedCache cache, ILogger<JwtBearerEventHandlers> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var principal = context.Principal;

        var userId =
            principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var verClaim = principal?.FindFirst("ver")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(verClaim))
        {
            context.HttpContext.Items["AuthError"] = "Invalid token: missing sub or ver claim.";
            _logger.LogWarning("JWT validation failed: missing sub or ver claim");
            context.Fail("Invalid token: missing sub or ver claim.");
            return;
        }

        var cacheKey = $"auth:token-version:{userId}";
        var cachedVersion = await _cache.GetStringAsync(cacheKey);

        if (string.IsNullOrEmpty(cachedVersion))
        {
            context.HttpContext.Items["AuthError"] = "Token is no longer valid (no version in cache).";
            _logger.LogInformation("JWT token version not found in Redis for user {UserId}", userId);
            context.Fail("Token is no longer valid.");
            return;
        }

        if (!string.Equals(cachedVersion, verClaim, StringComparison.Ordinal))
        {
            context.HttpContext.Items["AuthError"] = "Token is no longer valid (version mismatch).";
            _logger.LogInformation(
                "JWT token version mismatch for user {UserId}. Token: {TokenVersion}, Cache: {CachedVersion}",
                userId, verClaim, cachedVersion);
            context.Fail("Token is no longer valid (version mismatch).");
            return;
        }

        await base.TokenValidated(context);
    }

    public override Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();

        var errorMessage =
            context.HttpContext.Items["AuthError"] as string
            ?? "Invalid or expired token.";

        var error = new ApiErrorResponse
        {
            IsSuccess = false,
            Message = errorMessage,
            Error = "Unauthorized"
        };

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(error);
        return context.Response.WriteAsync(json);
    }
}