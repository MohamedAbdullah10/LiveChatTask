using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveChatTask.Application.Presence;
using LiveChatTask.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LiveChatTask.Services
{
    /// <summary>
    /// Presence business logic and persistence. No SignalR hub logic here.
    /// Broadcasting is done by the PresenceMonitor/Controller.
    /// </summary>
    public class PresenceService : IPresenceService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        // Keep last broadcasted status in-memory to detect changes efficiently.
        private static readonly ConcurrentDictionary<string, PresenceStatus> LastKnownStatus = new();

        // Ephemeral connection counts per userId (across all tabs/browsers).
        private static readonly ConcurrentDictionary<string, int> ActiveConnections = new();

        public PresenceService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private int IdleSeconds => _configuration.GetValue("Presence:IdleSeconds", 300);
        private int OfflineSeconds => _configuration.GetValue("Presence:OfflineSeconds", 45);

        public async Task UpdateHeartbeatAsync(string userId, string role)
        {
            var now = DateTime.UtcNow;

            // Only track presence for users and admins, but persistence is per user row.
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return;
            }

            // Mark online and update last seen.
            user.IsOnline = true;
            user.LastSeen = now;

            // Keep Role column consistent if needed
            if (!string.IsNullOrWhiteSpace(role) && user.Role != role)
            {
                user.Role = role;
            }

            await _context.SaveChangesAsync();
        }

        private PresenceStatus ComputeStatus(string userId, DateTime lastSeenUtc)
        {
            var now = DateTime.UtcNow;
            var offlineCutoff = now.AddSeconds(-OfflineSeconds);
            var idleCutoff = now.AddSeconds(-IdleSeconds);

            // If no recent heartbeat at all, treat as Offline regardless of connections.
            if (lastSeenUtc < offlineCutoff)
            {
                // Clean up any stale connection entries.
                ActiveConnections.TryRemove(userId, out _);
                return PresenceStatus.Offline;
            }

            // If we know they still have active connections, refine using idle threshold.
            if (ActiveConnections.TryGetValue(userId, out var connections) && connections > 0)
            {
                if (lastSeenUtc < idleCutoff)
                {
                    return PresenceStatus.Idle;
                }

                return PresenceStatus.Online;
            }

            // No active connections and lastSeen is still within offline window:
            // we consider the user Offline because there are no open sessions.
            return PresenceStatus.Offline;
        }

        public async Task<IReadOnlyList<(string UserId, string NameOrEmail, PresenceStatus Status, DateTime LastSeen)>> GetUserPresenceListAsync()
        {
            var users = await _context.Users
                .Where(u => u.Role == "User")
                .Select(u => new
                {
                    u.Id,
                    NameOrEmail = string.IsNullOrWhiteSpace(u.Email) ? u.UserName! : u.Email,
                    u.LastSeen
                })
                .ToListAsync();

            return users
                .Select(u => (u.Id, u.NameOrEmail, ComputeStatus(u.Id, u.LastSeen), u.LastSeen))
                .ToList();
        }

        public async Task<IReadOnlyList<(string UserId, PresenceStatus Status, DateTime LastSeen)>> DetectPresenceChangesAsync()
        {
            var users = await _context.Users
                .Where(u => u.Role == "User")
                .Select(u => new { u.Id, u.LastSeen })
                .ToListAsync();

            var changes = new List<(string UserId, PresenceStatus Status, DateTime LastSeen)>();

            foreach (var u in users)
            {
                var status = ComputeStatus(u.Id, u.LastSeen);

                if (!LastKnownStatus.TryGetValue(u.Id, out var last) || last != status)
                {
                    LastKnownStatus[u.Id] = status;
                    changes.Add((u.Id, status, u.LastSeen));
                }
            }

            return changes;
        }

        public Task ConnectionOpenedAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Task.CompletedTask;
            }

            ActiveConnections.AddOrUpdate(userId, 1, (_, current) => current + 1);
            return Task.CompletedTask;
        }

        public Task ConnectionClosedAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Task.CompletedTask;
            }

            if (ActiveConnections.TryGetValue(userId, out var current))
            {
                if (current <= 1)
                {
                    ActiveConnections.TryRemove(userId, out _);
                }
                else
                {
                    ActiveConnections[userId] = current - 1;
                }
            }

            return Task.CompletedTask;
        }
    }
}

