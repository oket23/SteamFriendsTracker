using Friends.Api.Dtos;
using Friends.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Friends.Api.Services;

public class FriendsUpdatesBackgroundService : BackgroundService
{
    private readonly ILogger<FriendsUpdatesBackgroundService> _logger;
    private readonly ActiveFriendsUsersService _activeUsers;
    private readonly IServiceScopeFactory _scopeFactory; 
    private readonly IHubContext<FriendsHub> _hubContext;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    
    public FriendsUpdatesBackgroundService(
        ILogger<FriendsUpdatesBackgroundService> logger, 
        ActiveFriendsUsersService activeUsers, 
        IServiceScopeFactory scopeFactory, 
        IHubContext<FriendsHub> hubContext)
    {
        _logger = logger;
        _activeUsers = activeUsers;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FriendsUpdatesBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var users = _activeUsers.GetAllUsers();

                if (users.Count > 0)
                {
                    _logger.LogDebug($"Processing friends updates for {users.Count} active users");
                }
                
                using (var scope = _scopeFactory.CreateScope()) 
                {
                    var snapshotService = scope.ServiceProvider.GetRequiredService<FriendsSnapshotService>();

                    foreach (var steamId in users)
                    {
                        if (string.IsNullOrWhiteSpace(steamId)) continue;
                        
                        FriendsSnapshotDto snapshot;

                        try
                        {
                            snapshot = await snapshotService.GetSnapshotAsync(steamId, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to get friends snapshot for user {SteamId}", steamId);
                            continue;
                        }

                        var groupName = FriendsHub.GetGroupName(steamId);

                        try
                        {
                            await _hubContext
                                .Clients
                                .Group(groupName)
                                .SendAsync("FriendsSnapshotUpdated", snapshot, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to send FriendsSnapshotUpdated to group {groupName} for user {steamId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in FriendsUpdatesBackgroundService loop");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
            }
        }

        _logger.LogInformation("FriendsUpdatesBackgroundService stopped");
    }
}