namespace Auth.Api.Models;

public class RefreshTokenData
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
}