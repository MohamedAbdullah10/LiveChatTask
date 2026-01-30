using System;
using System.Security.Claims;
using System.Threading.Tasks;
using LiveChatTask.Contracts.Chat;
using LiveChatTask.Hubs;
using LiveChatTask.Models;
using LiveChatTask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LiveChatTask.Controllers
{
    // Chat API - persists via IChatService, broadcasts via SignalR
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IChatSettingsService _settingsService;
        private readonly IFileUploadService _fileUploadService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(
            IChatService chatService,
            IChatSettingsService settingsService,
            IFileUploadService fileUploadService,
            IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _settingsService = settingsService;
            _fileUploadService = fileUploadService;
            _hubContext = hubContext;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private string GetRole() => User.IsInRole("Admin") ? "Admin" : "User";

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
        {
            try
            {
                var senderId = GetUserId();
                if (string.IsNullOrWhiteSpace(senderId))
                {
                    return Unauthorized();
                }

                var role = GetRole();

                // Char limit only for users (admins get more room)
                var maxLen = role == "User"
                    ? await _settingsService.GetMaxUserMessageLengthAsync()
                    : 5000;

                var command = new SendMessageCommand
                {
                    ChatSessionId = request.ChatSessionId,
                    SenderId = senderId,
                    Role = role,
                    Content = request.Text,
                    MessageType = request.MessageType
                };

                var result = await _chatService.SendMessageAsync(command, maxLen);
                var chatSessionKey = result.ChatSessionKey;

                var sentAt = result.CreatedAt.ToString("o");
                var userId = result.SenderId;
                var status = result.IsSeen ? "Seen" : "Sent";

                await _hubContext.Clients.Group(chatSessionKey)
                    .SendAsync("ReceiveMessage", chatSessionKey, result.MessageId, userId, request.Text, result.MessageType, role, sentAt, status);

                // Push unread count to admin dashboard badges
                if (role == "User" && !string.IsNullOrEmpty(result.SessionUserId) && result.UnreadCountForAdmin.HasValue)
                {
                    await _hubContext.Clients.Group(ChatHub.AdminPresenceGroup)
                        .SendAsync("UnreadCountChanged", result.SessionUserId, result.UnreadCountForAdmin.Value);
                }

                return Ok(new
                {
                    messageId = result.MessageId,
                    chatSessionId = chatSessionKey,
                    sentAt,
                    status = "Sent"
                });
            }
            catch (ArgumentException ex)
            {
                var role = GetRole();
                var maxLen = role == "User"
                    ? await _settingsService.GetMaxUserMessageLengthAsync()
                    : 5000;
                return BadRequest(new { message = ex.Message, maxLength = maxLen });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase))
            {
                await _hubContext.Clients.Group(request.ChatSessionId)
                    .SendAsync("SessionEnded", request.ChatSessionId, "DurationExpired");
                return BadRequest(new { message = "Your chat session has expired" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException)
            {
                return Unauthorized();
            }
        }

        // Admin inbox - all users with their session status
        [HttpGet("sessions")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Sessions()
        {
            var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Unauthorized();
            }

            var items = await _chatService.GetAdminSessionsAsync(adminId);
            var response = items.Select(x => new ChatSessionSummaryResponse
            {
                UserId = x.UserId,
                UserNameOrEmail = x.UserNameOrEmail,
                ChatSessionId = x.ChatSessionId,
                UnreadCount = x.UnreadCount,
                IsOnline = x.IsOnline,
                LastSeen = x.LastSeen
            });
            return Ok(response);
        }

        // Opens or creates a chat session for admin to talk to a user
        [HttpPost("open")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Open([FromBody] OpenChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest(new { message = "userId is required." });
            }

            var adminId = GetUserId();
            if (string.IsNullOrWhiteSpace(adminId))
            {
                return Unauthorized();
            }

            var session = await _chatService.GetOrCreateSessionAsync(request.UserId, adminId);
            return Ok(new { chatSessionId = session.SessionKey, userId = session.UserId });
        }

        // User initiates chat - creates session if needed
        [HttpGet("my-session")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> MySession()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var session = await _chatService.GetOrCreateUserSessionAsync(userId);
            return Ok(new ChatSessionResponse { ChatSessionId = session.SessionKey });
        }

        // For countdown timers on client
        [HttpGet("session-info")]
        public async Task<IActionResult> SessionInfo([FromQuery] string chatSessionId)
        {
            try
            {
                var requesterId = GetUserId();
                if (string.IsNullOrWhiteSpace(requesterId))
                {
                    return Unauthorized();
                }

                var role = GetRole();
                var session = await _chatService.GetSessionInfoAsync(chatSessionId, requesterId, role);
                
                if (session == null)
                {
                    return NotFound();
                }

                var currentMaxDurationMinutes = await _settingsService.GetMaxSessionDurationMinutesAsync();
                
                var now = DateTime.UtcNow;
                var elapsedMinutes = (now - session.StartedAt).TotalMinutes;
                var remainingMinutes = Math.Max(0, currentMaxDurationMinutes - elapsedMinutes);
                var isExpired = elapsedMinutes >= currentMaxDurationMinutes;

                return Ok(new ChatSessionInfoResponse
                {
                    ChatSessionId = session.SessionKey,
                    StartedAt = session.StartedAt,
                    MaxDurationMinutes = currentMaxDurationMinutes,
                    RemainingMinutes = remainingMinutes,
                    IsExpired = isExpired
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to get session info", error = ex.Message });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> History([FromQuery] string chatSessionId)
        {
            try
            {
                var requesterId = GetUserId();
                if (string.IsNullOrWhiteSpace(requesterId))
                {
                    return Unauthorized();
                }

                var items = await _chatService.GetHistoryAsync(requesterId, GetRole(), chatSessionId);
                return Ok(items);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // Read receipts - mark messages as seen and notify sender
        [HttpPost("mark-seen")]
        public async Task<IActionResult> MarkSeen([FromBody] MarkSeenRequest request)
        {
            try
            {
                var viewerId = GetUserId();
                if (string.IsNullOrWhiteSpace(viewerId))
                {
                    return Unauthorized();
                }

                var role = GetRole();

                if (string.IsNullOrWhiteSpace(request?.ChatSessionId))
                {
                    return BadRequest(new { message = "chatSessionId is required." });
                }

                var messageIds = await _chatService.MarkMessagesAsSeenAsync(request.ChatSessionId, viewerId, role);

                if (messageIds.Any())
                {
                    await _hubContext.Clients.Group(request.ChatSessionId)
                        .SendAsync("MessageStatusChanged", request.ChatSessionId, messageIds.ToArray(), "Seen");

                    // Reset admin dashboard badge
                    if (role == "Admin")
                    {
                        var session = await _chatService.GetSessionInfoAsync(request.ChatSessionId, viewerId, role);
                        if (session != null)
                        {
                            await _hubContext.Clients.Group(ChatHub.AdminPresenceGroup)
                                .SendAsync("UnreadCountChanged", session.UserId, 0);
                        }
                    }
                }

                return Ok(new { messageIds });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile( IFormFile file)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized();
                }

                var result = await _fileUploadService.UploadFileAsync(file);

                return Ok(new
                {
                    filePath = result.FilePath,
                    fileType = result.FileType,
                    fileName = result.FileName,
                    fileSize = result.FileSize
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "File upload failed", error = ex.Message });
            }
        }

        [HttpPost("upload-voice")]
        public async Task<IActionResult> UploadVoice( IFormFile file)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized();
                }

                var result = await _fileUploadService.UploadVoiceAsync(file);

                return Ok(new
                {
                    filePath = result.FilePath,
                    fileType = result.FileType,
                    fileName = result.FileName,
                    fileSize = result.FileSize
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Voice upload failed", error = ex.Message });
            }
        }
    }
}

