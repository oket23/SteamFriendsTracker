using System.Collections.Concurrent;

namespace Friends.Api.Services;

public class ActiveFriendsUsersService
{
    private readonly ConcurrentDictionary<string, byte> _activeUsers = new();

    public void AddUser(string steamId)
    {
        if (!string.IsNullOrWhiteSpace(steamId))
        {
            _activeUsers[steamId] = 0;
        }
    }

    public void RemoveUser(string steamId)
    {
        if (!string.IsNullOrWhiteSpace(steamId))
        {
            _activeUsers.TryRemove(steamId, out _);
        }
    }

    public IReadOnlyCollection<string> GetAllUsers()
    {
        return _activeUsers.Keys.ToList();
    }
}