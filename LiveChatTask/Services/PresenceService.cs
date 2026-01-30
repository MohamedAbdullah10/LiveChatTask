using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveChatTask.Contracts.Presence;
using LiveChatTask.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LiveChatTask.Services
{
    // Tracks online/idle/offline status. Broadcasting done separately by PresenceMonitor.
    public class PresenceService : IPresenceService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        // Cache last status to only broadcast when it actually changes
        private static readonly ConcurrentDictionary<string, PresenceStatus> LastKnownStatus = new();

        // Count open connections per user (multi-tab support)
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return;

            user.IsOnline = true;
            user.LastSeen = now;

            // Sync role if it drifted (shouldn't happen, but defensive)
            if (!string.IsNullOrWhiteSpace(role) && user.Role != role)
                user.Role = role;

            await _context.SaveChangesAsync();
        }

        private PresenceStatus ComputeStatus(string userId, DateTime lastSeenUtc)
        {
            var now = DateTime.UtcNow;
            var offlineCutoff = now.AddSeconds(-OfflineSeconds);
            var idleCutoff = now.AddSeconds(-IdleSeconds);

            // No heartbeat in too long = definitely offline
            if (lastSeenUtc < offlineCutoff)
            {
                ActiveConnections.TryRemove(userId, out _);
                return PresenceStatus.Offline;
            }

            // Has active connections? Check if idle based on last activity
            if (ActiveConnections.TryGetValue(userId, out var connections) && connections > 0)
                return lastSeenUtc < idleCutoff ? PresenceStatus.Idle : PresenceStatus.Online;

            // Within timeout but no connections = closed all tabs
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
                return Task.CompletedTask;

            ActiveConnections.AddOrUpdate(userId, 1, (_, current) => current + 1);
            return Task.CompletedTask;
        }

        public Task ConnectionClosedAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Task.CompletedTask;

            if (ActiveConnections.TryGetValue(userId, out var current))
            {
                if (current <= 1)
                    ActiveConnections.TryRemove(userId, out _);
                else
                    ActiveConnections[userId] = current - 1;
            }

            return Task.CompletedTask;
        }
    }
}

