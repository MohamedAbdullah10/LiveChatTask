using System.Collections.Generic;
using System.Threading.Tasks;
using LiveChatTask.Application.Presence;

namespace LiveChatTask.Services
{
    public interface IPresenceService
    {
        Task UpdateHeartbeatAsync(string userId, string role);
        Task<IReadOnlyList<(string UserId, string NameOrEmail, PresenceStatus Status, System.DateTime LastSeen)>> GetUserPresenceListAsync();

        /// <summary>
        /// Computes current presence statuses for all users and returns any changes since the last check.
        /// This is used by the background monitor to broadcast changes to admins.
        /// </summary>
        Task<IReadOnlyList<(string UserId, PresenceStatus Status, System.DateTime LastSeen)>> DetectPresenceChangesAsync();

        /// <summary>
        /// Notified by the SignalR hub when a connection is opened for a user.
        /// No hub logic here; this service keeps ephemeral connection counts.
        /// </summary>
        Task ConnectionOpenedAsync(string userId);

        /// <summary>
        /// Notified by the SignalR hub when a connection is closed for a user.
        /// </summary>
        Task ConnectionClosedAsync(string userId);
    }
}

