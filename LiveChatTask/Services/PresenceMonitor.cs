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
    /// <summary>
    /// Background monitor that detects presence changes and broadcasts them to admins.
    /// This keeps SignalR broadcasting outside the Hub and out of business logic.
    /// </summary>
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
                    // Hosted services are singletons; resolve scoped dependencies via a scope.
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
                    // Expected when application is shutting down
                    break;
                }
            }
        }
    }
}

