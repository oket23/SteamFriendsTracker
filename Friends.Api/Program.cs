using Friends.Api.Hubs;
using Friends.Api.Middleware;
using Friends.Api.Options;
using Friends.Api.Services;
using Serilog;

namespace Friends.Api;

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
                "{Timestamp:HH:mm:ss} [{Level:u3}] [FriendsMicroService] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                builder.Configuration["Logging:LogPath"],
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [FriendsMicroService]: {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        builder.Host.UseSerilog();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddControllers();

        builder.Services.Configure<SteamApiOptions>(builder.Configuration.GetSection("Steam"));
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<ActiveFriendsUsersService>();
        builder.Services.AddHostedService<FriendsUpdatesBackgroundService>();
        
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString");
        });
        builder.Services.AddHttpClient<SteamApiClient>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Steam:BaseUrl"]);
        });

        builder.Services.AddScoped<FriendsSnapshotService>();

        var app = builder.Build();
        app.UseMiddleware<UserIdentityMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapHub<FriendsHub>("/hubs/friends");

        app.MapControllers();

        app.Run();
    }
}