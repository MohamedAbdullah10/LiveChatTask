using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using LiveChatTask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace LiveChatTask.Hubs
{
    // Real-time messaging hub. Handles connection/group management only - 
    // all business logic lives in ChatService, called via API controllers.
    [Authorize]
    public class ChatHub : Hub
    {
        public const string AdminPresenceGroup = "admins";

        // Track connections per user (supports multiple tabs/devices)
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> UserConnections = new();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> ChatParticipants = new();

        private readonly IServiceScopeFactory _scopeFactory;

        public ChatHub(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // Adds client to a SignalR group so they receive messages for this chat
        public async Task JoinChat(string chatSessionId)
        {
            if (string.IsNullOrWhiteSpace(chatSessionId))
                return;

            var connectionId = Context.ConnectionId;
            var userId = Context.UserIdentifier ?? Context.User?.Identity?.Name ?? "anonymous";

            await Groups.AddToGroupAsync(connectionId, chatSessionId);

            var connections = UserConnections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            connections[connectionId] = 0;

            var participants = ChatParticipants.GetOrAdd(chatSessionId, _ => new ConcurrentDictionary<string, byte>());
            participants[userId] = 0;
        }

        public async Task LeaveChat(string chatSessionId)
        {
            if (string.IsNullOrWhiteSpace(chatSessionId))
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatSessionId);
        }

        // Admin dashboard subscribes to presence updates via this group
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
                    UserConnections.TryRemove(userId, out _);
            }

            // ChatParticipants cleanup skipped - would need connectionId->sessionId mapping

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

