using System;
using System.Threading;
using System.Threading.Tasks;
using LiveChatTask.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LiveChatTask.Services
{
    // Polls for presence changes every 10s and pushes updates to admin dashboard.
    // Keeps SignalR broadcasting out of business logic layer.
    public class PresenceMonitor : BackgroundService
    {
        public const string AdminPresenceGroup = "admins";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<PresenceMonitor> _logger;

        public PresenceMonitor(IServiceScopeFactory scopeFactory, IHubContext<ChatHub> hubContext, ILogger<PresenceMonitor> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var presenceService = scope.ServiceProvider.GetRequiredService<IPresenceService>();

                    var changes = await presenceService.DetectPresenceChangesAsync();
                    foreach (var change in changes)
                    {
                        await _hubContext.Clients.Group(AdminPresenceGroup)
                            .SendAsync(
                                "UserPresenceChanged",
                                change.UserId,
                                change.Status.ToString(),
                                change.LastSeen.ToString("o"),
                                cancellationToken: stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Presence monitor tick failed.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break; // Shutdown
                }
            }
        }
    }
}

