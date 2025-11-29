using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Shared.Models;

namespace Auth.Api.Authentication;

public class JwtBearerEventHandlers : JwtBearerEvents
{
    private readonly AuthDbService _authDbService;
    private readonly ILogger<JwtBearerEventHandlers> _logger;

    public JwtBearerEventHandlers(AuthDbService authDbService, ILogger<JwtBearerEventHandlers> logger)
    {
        _authDbService = authDbService;
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

        var user = await _authDbService.GetUserById(userId);

        if (user is null)
        {
            context.HttpContext.Items["AuthError"] = "User not found.";
            _logger.LogWarning("JWT validation failed: user {UserId} not found", userId);
            context.Fail("User not found.");
            return;
        }

        if (user.TokenVersion.ToString() != verClaim)
        {
            context.HttpContext.Items["AuthError"] = "Token is no longer valid (version mismatch).";
            _logger.LogInformation("JWT token version mismatch for user {UserId}", userId);
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