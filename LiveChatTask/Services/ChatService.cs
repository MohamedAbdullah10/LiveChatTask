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

        public ChatService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ChatSession> GetOrCreateSessionAsync(string userId, string adminId)
        {
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

                await _context.SaveChangesAsync();
                return existing;
            }

            var session = new ChatSession
            {
                SessionKey = Guid.NewGuid().ToString("N"),
                UserId = userId,
                AdminId = adminId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastUserMessageAt = DateTime.UtcNow
            };

            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<ChatSession> GetOrCreateUserSessionAsync(string userId)
        {
            var existing = await _context.ChatSessions
                .FirstOrDefaultAsync(cs => cs.IsActive && cs.UserId == userId);

            if (existing != null)
            {
                if (string.IsNullOrWhiteSpace(existing.SessionKey))
                {
                    existing.SessionKey = Guid.NewGuid().ToString("N");
                    await _context.SaveChangesAsync();
                }

                return existing;
            }

            var session = new ChatSession
            {
                SessionKey = Guid.NewGuid().ToString("N"),
                UserId = userId,
                AdminId = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastUserMessageAt = DateTime.UtcNow
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

            // TODO: when implementing idle timeout, update LastUserMessageAt here for user messages
            // and use it to trigger automatic termination warnings.

            await _context.SaveChangesAsync();

            return new SendMessageResult
            {
                Message = message,
                ChatSessionKey = chatSession.SessionKey,
                Role = command.Role
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
    }
}

