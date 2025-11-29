using Game.Api.Services;
using Serilog;

namespace Game.Api;

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
                "{Timestamp:HH:mm:ss} [{Level:u3}] [GameMicroService] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                builder.Configuration["Logging:LogPath"],
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [GameMicroService]: {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        builder.Host.UseSerilog();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddControllers();

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString");
        });

        builder.Services.AddHttpClient<SteamGameApiService>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["SteamApi:BaseUrl"]);
        });
        builder.Services.AddHttpClient<SteamStoreApiService>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["SteamApi:BaseUrl"]);
        });

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}