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
    /// Background monitor that sends idle termination messages when the user has not responded for 1 minute.
    /// Broadcasts the message and SessionEnded via SignalR so both Admin and User clients receive them in real time.
    /// </summary>
    public class IdleChatMonitor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<IdleChatMonitor> _logger;

        public IdleChatMonitor(IServiceScopeFactory scopeFactory, IHubContext<ChatHub> hubContext, ILogger<IdleChatMonitor> logger)
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
                    var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

                    var sessionKeys = await chatService.GetSessionKeysForIdleTerminationAsync();
                    foreach (var sessionKey in sessionKeys)
                    {
                        var result = await chatService.SendIdleTerminationIfNeededAsync(sessionKey);
                        if (result == null)
                            continue;

                        var sentAt = result.CreatedAt.ToString("o");
                        await _hubContext.Clients.Group(result.SessionKey)
                            .SendAsync(
                                "ReceiveMessage",
                                result.SessionKey,
                                result.MessageId,
                                result.SenderId,
                                result.Content,
                                "System",
                                "System",
                                sentAt,
                                "Sent",
                                cancellationToken: stoppingToken);

                        await _hubContext.Clients.Group(result.SessionKey)
                            .SendAsync("SessionEnded", result.SessionKey, "IdleTerminated", cancellationToken: stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Idle chat monitor tick failed.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}
