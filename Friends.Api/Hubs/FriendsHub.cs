using Friends.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Friends.Api.Hubs;

public class FriendsHub : Hub
{
    private readonly ILogger<FriendsHub> _logger;
    private readonly ActiveFriendsUsersService _activeUsers;

    public FriendsHub(
        ILogger<FriendsHub> logger,
        ActiveFriendsUsersService activeUsers)
    {
        _logger = logger;
        _activeUsers = activeUsers;
    }

    public override async Task OnConnectedAsync()
    {
        var steamId = Context.User?.FindFirst("steamId")?.Value;

        if (string.IsNullOrWhiteSpace(steamId))
        {
            _logger.LogWarning($"Connection {Context.ConnectionId} has no steamId claim. Aborting.");
            Context.Abort();
            return;
        }

        var groupName = GetGroupName(steamId);

        _activeUsers.AddUser(steamId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation($"Connection {Context.ConnectionId} joined friends group {groupName} for user {steamId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var steamId = Context.User?.FindFirst("steamId")?.Value;

        if (!string.IsNullOrWhiteSpace(steamId))
        {
            var groupName = GetGroupName(steamId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            _activeUsers.RemoveUser(steamId);

            _logger.LogInformation($"Connection {Context.ConnectionId} left friends group {groupName} for user {steamId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static string GetGroupName(string steamId)
    {
        return $"friends:{steamId}";
    }
}
