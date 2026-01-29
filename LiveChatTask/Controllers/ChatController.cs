using System;
using System.Security.Claims;
using System.Threading.Tasks;
using LiveChatTask.Application.Chat;
using LiveChatTask.Contracts.Chat;
using LiveChatTask.Hubs;
using LiveChatTask.Models;
using LiveChatTask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LiveChatTask.Controllers
{
    /// <summary>
    /// API controller responsible for coordinating chat service and broadcasting via SignalR.
    /// SignalR hub is used only for real-time delivery; business logic lives in IChatService.
    /// </summary>
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

        /// <summary>
        /// Sends a message in a chat session: delegates to IChatService and broadcasts via SignalR.
        /// POST: /api/chat/send
        /// </summary>
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

                // Admin-configurable limit (applies to USER messages only).
                var maxLen = role == "User"
                    ? await _settingsService.GetMaxUserMessageLengthAsync()
                    : 5000;

                var messageType = Enum.TryParse<MessageType>(request.MessageType, ignoreCase: true, out var mt) ? mt : MessageType.Text;

                var command = new SendMessageCommand
                {
                    ChatSessionId = request.ChatSessionId,
                    SenderId = senderId,
                    Role = role,
                    Content = request.Text,
                    MessageType = messageType
                };

                var result = await _chatService.SendMessageAsync(command, maxLen);
                var message = result.Message;
                var chatSessionKey = result.ChatSessionKey;

                var sentAt = message.CreatedAt.ToString("o");
                var userId = message.SenderId;
                var status = message.IsSeen ? "Seen" : "Sent";

                // Broadcast using persisted message type (single source of truth).
                await _hubContext.Clients.Group(chatSessionKey)
                    .SendAsync("ReceiveMessage", chatSessionKey, message.Id, userId, request.Text, message.Type.ToString(), role, sentAt, status);

                return Ok(new
                {
                    messageId = message.Id,
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
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException)
            {
                return Unauthorized();
            }
        }

        /// <summary>
        /// Admin-only: returns a list of users with their current session key (chatSessionId) if exists.
        /// GET: /api/chat/sessions
        /// </summary>
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

        /// <summary>
        /// Admin-only: open (or create) a chat session for a user and return its SessionKey as chatSessionId.
        /// POST: /api/chat/open
        /// </summary>
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

        /// <summary>
        /// User-only: gets (or creates) the current user's active session and returns its SessionKey.
        /// GET: /api/chat/my-session
        /// </summary>
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

        /// <summary>
        /// Returns a simple history of messages for a given chatSessionId (SessionKey).
        /// GET: /api/chat/history?chatSessionId=...
        /// </summary>
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
                var response = items.Select(x => new ChatHistoryItemResponse
                {
                    Id = x.Id,
                    Content = x.Content,
                    CreatedAt = x.CreatedAt,
                    IsSeen = x.IsSeen,
                    Role = x.Role,
                    SenderId = x.SenderId,
                    MessageType = x.MessageType.ToString()
                });
                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Marks all unseen messages in a chat session as seen (for the viewer).
        /// When a user views messages, they are marked as seen and admin is notified via SignalR.
        /// POST: /api/chat/mark-seen
        /// </summary>
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
                    // Broadcast status change to all clients in this chat session group
                    await _hubContext.Clients.Group(request.ChatSessionId)
                        .SendAsync("MessageStatusChanged", request.ChatSessionId, messageIds.ToArray(), "Seen");
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

        /// <summary>
        /// Uploads a file (image or document) for use in chat messages.
        /// POST: /api/chat/upload-file
        /// </summary>
        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
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

        /// <summary>
        /// Uploads a voice recording (audio file) for use in chat messages.
        /// POST: /api/chat/upload-voice
        /// </summary>
        [HttpPost("upload-voice")]
        public async Task<IActionResult> UploadVoice([FromForm] IFormFile file)
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

