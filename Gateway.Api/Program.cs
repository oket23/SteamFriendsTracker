using Gateway.Api.Authentication;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString");
        });

        builder.Services.AddJwtAuth(builder.Configuration);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy
                    .WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI(x =>
        {
            x.SwaggerEndpoint("/auth-api/swagger/v1/swagger.json", "Auth API");
            x.SwaggerEndpoint("/friends-api/swagger/v1/swagger.json", "Friends API");
            x.SwaggerEndpoint("/games-api/swagger/v1/swagger.json", "Games API");
        });

        app.UseHttpsRedirection();

        app.UseCors("Frontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.UseWebSockets();
        app.MapReverseProxy(proxyPipeline =>
        {
            proxyPipeline.Use(async (context, next) =>
            {
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    var steamId = context.User.FindFirst("sub")?.Value;

                    if (!string.IsNullOrEmpty(steamId)) context.Request.Headers["X-User-Id"] = steamId;
                }

                await next();
            });
        });

        app.Run();
    }
}