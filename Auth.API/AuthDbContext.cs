using Auth.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Auth.API.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    public DbSet<DbUserDto> Users => Set<DbUserDto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DbUserDto>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
        
    }
}
