using System.Collections.Generic;
using System.Threading.Tasks;
using LiveChatTask.Contracts.Presence;

namespace LiveChatTask.Services
{
    public interface IPresenceService
    {
        Task UpdateHeartbeatAsync(string userId, string role);
        Task<IReadOnlyList<(string UserId, string NameOrEmail, PresenceStatus Status, System.DateTime LastSeen)>> GetUserPresenceListAsync();

        // Returns only users whose status changed since last check (for efficient broadcasting)
        Task<IReadOnlyList<(string UserId, PresenceStatus Status, System.DateTime LastSeen)>> DetectPresenceChangesAsync();

        // Called by ChatHub on connect/disconnect to track active tabs
        Task ConnectionOpenedAsync(string userId);
        Task ConnectionClosedAsync(string userId);
    }
}

