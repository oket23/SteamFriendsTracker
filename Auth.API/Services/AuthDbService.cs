using Auth.API.Data;
using Auth.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Services;

public class AuthDbService
{
    private readonly AuthDbContext _dbContext;
    private readonly ILogger<AuthDbService> _logger;

    public AuthDbService(AuthDbContext dbContext, ILogger<AuthDbService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<DbUserDto> GetUserById(string id)
    {
        return _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task CreateUserAsync(DbUserDto user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(DbUserDto user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }

    public Task<DbUserDto> GetUserByRefreshTokenAsync(string refreshToken)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
    }
}