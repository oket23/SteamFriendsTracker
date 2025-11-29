using Auth.Api.Authentication;
using Auth.API.Data;
using Auth.Api.Options;
using Auth.Api.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Auth.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate:
                "{Timestamp:HH:mm:ss} [{Level:u3}] [AuthMicroService] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                builder.Configuration["Logging:LogPath"],
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [AuthMicroService]: {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        builder.Host.UseSerilog();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddControllers();

        builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
        builder.Services.Configure<SteamApiOptions>(builder.Configuration.GetSection("Steam"));

        builder.Services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(builder.Configuration["Postgres:ConnectionString"]));
        builder.Services.AddStackExchangeRedisCache(options =>
            options.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString"));

        builder.Services.AddHttpClient<AuthService>(client =>
            client.BaseAddress = new Uri(builder.Configuration["SteamApi:BaseUrl"]));
        builder.Services.AddHttpClient<SteamApiService>();

        builder.Services.AddScoped<AuthDbService>();

        builder.Services.AddJwtAuth(builder.Configuration);

        builder.Services.AddAuthorization();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}