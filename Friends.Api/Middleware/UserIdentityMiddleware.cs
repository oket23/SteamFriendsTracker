using System.Security.Claims;

namespace Friends.Api.Middleware;

public class UserIdentityMiddleware
{
    private readonly RequestDelegate _next;

    public UserIdentityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-User-Id", out var userId) && !string.IsNullOrEmpty(userId))
        {
            var claims = new[]
            {
                new Claim("steamId", userId.ToString()), 
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "Gateway"); 
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}