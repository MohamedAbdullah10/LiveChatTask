using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveChatTask.Application.Chat;
using LiveChatTask.Data;
using LiveChatTask.Models;
using Microsoft.EntityFrameworkCore;

namespace LiveChatTask.Services
{
    /// <summary>
    /// Handles chat-related business logic: validation, session resolution and message persistence.
    /// Broadcasting is handled by the API controller via SignalR hub.
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;
        private readonly IChatSettingsService _settingsService;

        public ChatService(AppDbContext context, IChatSettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task<ChatSession> GetOrCreateSessionAsync(string userId, string adminId)
        {
            var now = DateTime.UtcNow;
            var defaultMaxDurationMinutes = await _settingsService.GetMaxSessionDurationMinutesAsync();

            // Prefer an existing session already assigned to this admin; otherwise allow claiming an unassigned session.
            var existing = await _context.ChatSessions
                .FirstOrDefaultAsync(cs =>
                    cs.IsActive &&
                    cs.UserId == userId &&
                    (cs.AdminId == adminId || cs.AdminId == null));

            if (existing != null)
            {
                if (string.IsNullOrWhiteSpace(existing.SessionKey))
                {
                    existing.SessionKey = Guid.NewGuid().ToString("N");
                }

                if (existing.AdminId == null)
                {
                    existing.AdminId = adminId;
                }

                // Update MaxDurationMinutes to match current setting (admin may have changed it)
                if (existing.MaxDurationMinutes != defaultMaxDurationMinutes)
                {
                    existing.MaxDurationMinutes = defaultMaxDurationMinutes;
                }

                // Ensure StartedAt is set
                if (existing.StartedAt == default || existing.StartedAt == DateTime.MinValue)
                {
                    existing.StartedAt = existing.CreatedAt > now ? now : existing.CreatedAt;
                }

                // Don't reset expired sessions automatically - let expiration be enforced
                // The expiration will be checked in SendMessageAsync and GetSessionInfoAsync
                await _context.SaveChangesAsync();
                return existing;
            }

            var session = new ChatSession
            {
                SessionKey = Guid.NewGuid().ToString("N"),
                UserId = userId,
                AdminId = adminId,
                IsActive = true,
                CreatedAt = now,
                StartedAt = now,
                MaxDurationMinutes = defaultMaxDurationMinutes,
                LastUserMessageAt = now
            };

            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<ChatSession> GetOrCreateUserSessionAsync(string userId)
        {
            var now = DateTime.UtcNow;
            var defaultMaxDurationMinutes = await _settingsService.GetMaxSessionDurationMinutesAsync();

            var existing = await _context.ChatSessions
                .FirstOrDefaultAsync(cs => cs.IsActive && cs.UserId == userId);

            if (existing != null)
            {
                if (string.IsNullOrWhiteSpace(existing.SessionKey))
                {
                    existing.SessionKey = Guid.NewGuid().ToString("N");
                }

                // Update MaxDurationMinutes to match current setting (admin may have changed it)
                if (existing.MaxDurationMinutes != defaultMaxDurationMinutes)
                {
                    existing.MaxDurationMinutes = defaultMaxDurationMinutes;
                }

                // Ensure StartedAt is set
                if (existing.StartedAt == default || existing.StartedAt == DateTime.MinValue)
                {
                    existing.StartedAt = existing.CreatedAt > now ? now : existing.CreatedAt;
                }

                // If current session is expired, create a new session (for "Start New Chat Session" flow)
                if (defaultMaxDurationMinutes > 0)
                {
                    var elapsedMinutes = (now - existing.StartedAt).TotalMinutes;
                    if (elapsedMinutes >= defaultMaxDurationMinutes)
                    {
                        existing.IsActive = false;
                        await _context.SaveChangesAsync();

                        var newSession = new ChatSession
                        {
                            SessionKey = Guid.NewGuid().ToString("N"),
                            UserId = userId,
                            AdminId = null,
                            IsActive = true,
                            CreatedAt = now,
                            StartedAt = now,
                            MaxDurationMinutes = defaultMaxDurationMinutes,
                            LastUserMessageAt = now
                        };
                        _context.ChatSessions.Add(newSession);
                        await _context.SaveChangesAsync();
                        return newSession;
                    }
                }

                await _context.SaveChangesAsync();
                return existing;
            }

            var session = new ChatSession
            {
                SessionKey = Guid.NewGuid().ToString("N"),
                UserId = userId,
                AdminId = null,
                IsActive = true,
                CreatedAt = now,
                StartedAt = now,
                MaxDurationMinutes = defaultMaxDurationMinutes,
                LastUserMessageAt = now
            };

            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<IReadOnlyList<ChatSessionSummaryModel>> GetAdminSessionsAsync(string adminId)
        {
            // Return all users with any active session (assigned to this admin or unassigned).
            // This supports an inbox-like list where admin can click a user to open/claim the chat.

            var users = await _context.Users
                .Where(u => u.Role == "User")
                .Select(u => new
                {
                    u.Id,
                    NameOrEmail = string.IsNullOrWhiteSpace(u.Email) ? u.UserName! : u.Email,
                    u.IsOnline,
                    u.LastSeen
                })
                .ToListAsync();

            var sessions = await _context.ChatSessions
                .Where(cs => cs.IsActive && (cs.AdminId == adminId || cs.AdminId == null))
                .Select(cs => new
                {
                    cs.UserId,
                    cs.SessionKey,
                    cs.Id
                })
                .ToListAsync();

            // Unread = messages in session not seen and not sent by admin.
            // (Lightweight; can be optimized later.)
            var sessionIds = sessions.Select(s => s.Id).ToList();
            var unreadByUser = await _context.Messages
                .Where(m => sessionIds.Contains(m.ChatSessionId) && !m.IsSeen && m.SenderId != adminId)
                .GroupBy(m => m.ChatSession.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            return users.Select(u =>
            {
                var session = sessions.FirstOrDefault(s => s.UserId == u.Id);
                var unread = unreadByUser.FirstOrDefault(x => x.UserId == u.Id)?.Count ?? 0;

                return new ChatSessionSummaryModel
                {
                    UserId = u.Id,
                    UserNameOrEmail = u.NameOrEmail,
                    ChatSessionId = session?.SessionKey,
                    UnreadCount = unread,
                    IsOnline = u.IsOnline,
                    LastSeen = u.LastSeen
                };
            }).ToList();
        }

        public async Task<SendMessageResult> SendMessageAsync(SendMessageCommand command, int maxMessageLength)
        {
            if (command == null)
            {
                throw new ArgumentException("Command is required.", nameof(command));
            }

            if (string.IsNullOrWhiteSpace(command.Content))
            {
                throw new ArgumentException("Message text is required.", nameof(command));
            }

            if (command.Content.Length > maxMessageLength)
            {
                throw new ArgumentException(
                    $"Message exceeds maximum length of {maxMessageLength} characters.",
                    nameof(command));
            }

            if (string.IsNullOrWhiteSpace(command.ChatSessionId))
            {
                throw new ArgumentException("chatSessionId is required.", nameof(command));
            }

            if (string.IsNullOrWhiteSpace(command.SenderId))
            {
                throw new InvalidOperationException("SenderId is required.");
            }

            if (command.Role != "Admin" && command.Role != "User")
            {
                throw new ArgumentException("Role must be 'Admin' or 'User'.", nameof(command));
            }

            // Ensure sender exists
            var senderExists = await _context.Users.AnyAsync(u => u.Id == command.SenderId);
            if (!senderExists)
            {
                throw new InvalidOperationException("Sender does not exist.");
            }

            var chatSession = await _context.ChatSessions
                .FirstOrDefaultAsync(cs => cs.IsActive && cs.SessionKey == command.ChatSessionId);

            if (chatSession == null)
            {
                throw new ArgumentException("Chat session not found.", nameof(command));
            }

            // Authorization: users can only access their own sessions; admins can only access sessions assigned to them,
            // or claim unassigned sessions when they first open them.
            if (command.Role == "User" && chatSession.UserId != command.SenderId)
            {
                throw new UnauthorizedAccessException();
            }

            if (command.Role == "Admin")
            {
                if (chatSession.AdminId == null)
                {
                    chatSession.AdminId = command.SenderId; // claim session
                }
                else if (chatSession.AdminId != command.SenderId)
                {
                    throw new UnauthorizedAccessException();
                }
            }

            // Check session duration for User messages (admins can always send)
            if (command.Role == "User")
            {
                // Ensure StartedAt is set (for existing sessions created before this feature)
                if (chatSession.StartedAt == default)
                {
                    chatSession.StartedAt = chatSession.CreatedAt;
                }

                // Always use the CURRENT setting value (admin may have changed it)
                var currentMaxDurationMinutes = await _settingsService.GetMaxSessionDurationMinutesAsync();
                
                // Update stored value to match current setting
                if (chatSession.MaxDurationMinutes != currentMaxDurationMinutes)
                {
                    chatSession.MaxDurationMinutes = currentMaxDurationMinutes;
                    await _context.SaveChangesAsync();
                }

                // Check expiration using CURRENT setting value
                if (currentMaxDurationMinutes > 0)
                {
                    var elapsedMinutes = (DateTime.UtcNow - chatSession.StartedAt).TotalMinutes;
                    if (elapsedMinutes >= currentMaxDurationMinutes)
                    {
                        throw new InvalidOperationException("Your chat session has expired");
                    }
                }
            }

            var message = new Message
            {
                SenderId = command.SenderId,
                ChatSessionId = chatSession.Id,
                Content = command.Content,
                Type = command.MessageType,
                IsSeen = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);

            if (command.Role == "User")
            {
                chatSession.LastUserMessageAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            int? unreadCountForAdmin = null;
            if (command.Role == "User")
            {
                unreadCountForAdmin = await _context.Messages
                    .CountAsync(m => m.ChatSessionId == chatSession.Id && m.SenderId == chatSession.UserId && !m.IsSeen);
            }

            return new SendMessageResult
            {
                Message = message,
                ChatSessionKey = chatSession.SessionKey,
                Role = command.Role,
                SessionUserId = command.Role == "User" ? chatSession.UserId : null,
                UnreadCountForAdmin = unreadCountForAdmin
            };
        }

        public async Task<IReadOnlyList<ChatHistoryItemModel>> GetHistoryAsync(string requesterId, string requesterRole, string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey))
            {
                return Array.Empty<ChatHistoryItemModel>();
            }

            if (string.IsNullOrWhiteSpace(requesterId))
            {
                throw new InvalidOperationException("RequesterId is required.");
            }

            if (requesterRole != "Admin" && requesterRole != "User")
            {
                throw new ArgumentException("Requester role must be 'Admin' or 'User'.", nameof(requesterRole));
            }

            var chatSession = await _context.ChatSessions
                .FirstOrDefaultAsync(cs => cs.IsActive && cs.SessionKey == chatSessionKey);

            if (chatSession == null)
            {
                return Array.Empty<ChatHistoryItemModel>();
            }

            if (requesterRole == "User" && chatSession.UserId != requesterId)
            {
                throw new UnauthorizedAccessException();
            }

            if (requesterRole == "Admin")
            {
                if (chatSession.AdminId != null && chatSession.AdminId != requesterId)
                {
                    throw new UnauthorizedAccessException();
                }
            }

            var messages = await _context.Messages
                .Where(m => m.ChatSessionId == chatSession.Id)
                .OrderBy(m => m.CreatedAt)
                .Take(100)
                .Select(m => new ChatHistoryItemModel
                {
                    Id = m.Id,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    IsSeen = m.IsSeen,
                    Role = m.Sender.Role,
                    SenderId = m.SenderId,
                    MessageType = m.Type
                })
                .ToListAsync();

            return messages;
        }

        public async Task<IReadOnlyList<int>> MarkMessagesAsSeenAsync(string chatSessionKey, string viewerId, string viewerRole)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey))
            {
                return Array.Empty<int>();
            }

            if (string.IsNullOrWhiteSpace(viewerId))
            {
                throw new InvalidOperationException("ViewerId is required.");
            }

            if (viewerRole != "Admin" && viewerRole != "User")
            {
                throw new ArgumentException("Viewer role must be 'Admin' or 'User'.", nameof(viewerRole));
            }

            var chatSession = await _context.ChatSessions
                .FirstOrDefaultAsync(cs => cs.IsActive && cs.SessionKey == chatSessionKey);

            if (chatSession == null)
            {
                return Array.Empty<int>();
            }

            // Authorization: users can only mark messages in their own sessions
            if (viewerRole == "User" && chatSession.UserId != viewerId)
            {
                throw new UnauthorizedAccessException();
            }

            // Admins can mark messages in sessions assigned to them
            if (viewerRole == "Admin")
            {
                if (chatSession.AdminId != null && chatSession.AdminId != viewerId)
                {
                    throw new UnauthorizedAccessException();
                }
            }

            // Mark all unseen messages from the other party as seen
            // User marks Admin messages as seen, Admin marks User messages as seen
            var otherPartyId = viewerRole == "User" ? chatSession.AdminId : chatSession.UserId;
            
            if (string.IsNullOrWhiteSpace(otherPartyId))
            {
                return Array.Empty<int>();
            }

            var messagesToMark = await _context.Messages
                .Where(m => m.ChatSessionId == chatSession.Id 
                    && m.SenderId == otherPartyId 
                    && !m.IsSeen)
                .ToListAsync();

            var messageIds = messagesToMark.Select(m => m.Id).ToList();

            foreach (var msg in messagesToMark)
            {
                msg.IsSeen = true;
            }

            if (messagesToMark.Any())
            {
                await _context.SaveChangesAsync();
            }

            return messageIds;
        }

        public async Task<ChatSession?> GetSessionInfoAsync(string chatSessionKey, string requesterId, string requesterRole)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey) || string.IsNullOrWhiteSpace(requesterId))
            {
                return null;
            }

            var chatSession = await _context.ChatSessions
                .FirstOrDefaultAsync(cs => cs.IsActive && cs.SessionKey == chatSessionKey);

            if (chatSession == null)
            {
                return null;
            }

            // Authorization check
            if (requesterRole == "User" && chatSession.UserId != requesterId)
            {
                return null;
            }

            if (requesterRole == "Admin")
            {
                if (chatSession.AdminId != null && chatSession.AdminId != requesterId)
                {
                    return null;
                }
            }

            var now = DateTime.UtcNow;
            // Always use the current setting value (admin may have changed it)
            var currentMaxDurationMinutes = await _settingsService.GetMaxSessionDurationMinutesAsync();
            
            // Update stored value to match current setting (for consistency)
            if (chatSession.MaxDurationMinutes != currentMaxDurationMinutes)
            {
                chatSession.MaxDurationMinutes = currentMaxDurationMinutes;
            }

            // Ensure StartedAt is set
            if (chatSession.StartedAt == default || chatSession.StartedAt == DateTime.MinValue)
            {
                // If StartedAt was never set, use CreatedAt or now (whichever is more recent)
                chatSession.StartedAt = chatSession.CreatedAt > now ? now : chatSession.CreatedAt;
            }

            // Don't reset StartedAt here - let the expiration stand
            // The expiration check is done in the controller/UI
            // Save any changes (MaxDurationMinutes update)
            await _context.SaveChangesAsync();

            return chatSession;
        }

        public async Task<IReadOnlyList<string>> GetSessionKeysForIdleTerminationAsync()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-1);
            var sessionKeys = await _context.ChatSessions
                .Where(cs => cs.IsActive
                    && cs.IdleTerminationSentAt == null
                    && cs.LastUserMessageAt < cutoff)
                .OrderBy(cs => cs.LastUserMessageAt)
                .Take(20)
                .Select(cs => cs.SessionKey)
                .ToListAsync();
            return sessionKeys;
        }

        public async Task<IdleTerminationResult?> SendIdleTerminationIfNeededAsync(string sessionKey)
        {
            if (string.IsNullOrWhiteSpace(sessionKey))
            {
                return null;
            }

            var chatSession = await _context.ChatSessions
                .FirstOrDefaultAsync(cs => cs.SessionKey == sessionKey);

            if (chatSession == null || !chatSession.IsActive || chatSession.IdleTerminationSentAt != null)
            {
                return null;
            }

            var cutoff = DateTime.UtcNow.AddMinutes(-1);
            if (chatSession.LastUserMessageAt >= cutoff)
            {
                return null;
            }

            var systemUser = await _context.Set<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Role == "System");
            if (systemUser == null)
            {
                return null;
            }

            const string content = "The chat will be terminated because we have not received a response from you.";
            var now = DateTime.UtcNow;

            var message = new Message
            {
                SenderId = systemUser.Id,
                ChatSessionId = chatSession.Id,
                Content = content,
                Type = MessageType.System,
                IsSeen = false,
                CreatedAt = now
            };

            _context.Messages.Add(message);
            chatSession.IdleTerminationSentAt = now;
            chatSession.IsActive = false;
            await _context.SaveChangesAsync();

            return new IdleTerminationResult
            {
                SessionKey = chatSession.SessionKey,
                MessageId = message.Id,
                Content = content,
                CreatedAt = now,
                SenderId = systemUser.Id
            };
        }
    }
}

