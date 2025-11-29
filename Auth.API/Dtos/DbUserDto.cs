namespace Auth.Api.Dtos;

public class DbUserDto
{
    public string Id { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TokenVersion { get; set; } = 1;
}