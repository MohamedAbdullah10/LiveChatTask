using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using LiveChatTask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace LiveChatTask.Hubs
{
    /// <summary>
    /// SignalR hub for real-time chat between users and admins.
    /// Responsible only for connection management and broadcasting.
    /// All persistence and business logic remains in API controllers.
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        public const string AdminPresenceGroup = "admins";

        // userId -> set of connectionIds
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> UserConnections
            = new();

        // chatSessionId -> set of userIds
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> ChatParticipants
            = new();

        private readonly IServiceScopeFactory _scopeFactory;

        public ChatHub(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// A client joins a chat session (group) identified by chatSessionId.
        /// Both admin and user will join the same group to receive messages.
        /// </summary>
        public async Task JoinChat(string chatSessionId)
        {
            if (string.IsNullOrWhiteSpace(chatSessionId))
            {
                return;
            }

            var connectionId = Context.ConnectionId;
            var userId = Context.UserIdentifier ?? Context.User?.Identity?.Name ?? "anonymous";

            await Groups.AddToGroupAsync(connectionId, chatSessionId);

            // Track user connections
            var connections = UserConnections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            connections[connectionId] = 0;

            // Track chat participants
            var participants = ChatParticipants.GetOrAdd(chatSessionId, _ => new ConcurrentDictionary<string, byte>());
            participants[userId] = 0;
        }

        /// <summary>
        /// A client leaves a chat session (group).
        /// </summary>
        public async Task LeaveChat(string chatSessionId)
        {
            if (string.IsNullOrWhiteSpace(chatSessionId))
            {
                return;
            }

            var connectionId = Context.ConnectionId;
            await Groups.RemoveFromGroupAsync(connectionId, chatSessionId);
        }

        /// <summary>
        /// Admin UI joins a dedicated group to receive presence notifications.
        /// This is group management only.
        /// </summary>
        public Task JoinAdminPresence()
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, AdminPresenceGroup);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            var userId = Context.UserIdentifier ?? Context.User?.Identity?.Name ?? "anonymous";

            if (UserConnections.TryGetValue(userId, out var connections))
            {
                connections.TryRemove(connectionId, out _);

                if (connections.IsEmpty)
                {
                    UserConnections.TryRemove(userId, out _);
                }
            }

            // We do not currently remove from ChatParticipants, as that would
            // require mapping connectionIds to chatSessionIds.

            // Inform presence service that a connection was closed.
            using (var scope = _scopeFactory.CreateScope())
            {
                var presenceService = scope.ServiceProvider.GetRequiredService<IPresenceService>();
                _ = presenceService.ConnectionClosedAsync(userId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier ?? Context.User?.Identity?.Name ?? "anonymous";

            using (var scope = _scopeFactory.CreateScope())
            {
                var presenceService = scope.ServiceProvider.GetRequiredService<IPresenceService>();
                _ = presenceService.ConnectionOpenedAsync(userId);
            }

            return base.OnConnectedAsync();
        }
    }
}

