using System.Collections.Generic;
using System.Threading.Tasks;
using LiveChatTask.Application.Chat;
using LiveChatTask.Models;

namespace LiveChatTask.Services
{
    public interface IChatService
    {
        Task<ChatSession> GetOrCreateSessionAsync(string userId, string adminId);

        Task<ChatSession> GetOrCreateUserSessionAsync(string userId);

        Task<IReadOnlyList<ChatSessionSummaryModel>> GetAdminSessionsAsync(string adminId);

        Task<SendMessageResult> SendMessageAsync(SendMessageCommand command, int maxMessageLength);

        Task<IReadOnlyList<ChatHistoryItemModel>> GetHistoryAsync(string requesterId, string requesterRole, string chatSessionKey);

        Task<IReadOnlyList<int>> MarkMessagesAsSeenAsync(string chatSessionKey, string viewerId, string viewerRole);

        Task<ChatSession?> GetSessionInfoAsync(string chatSessionKey, string requesterId, string requesterRole);

        Task<IReadOnlyList<string>> GetSessionKeysForIdleTerminationAsync();

        Task<IdleTerminationResult?> SendIdleTerminationIfNeededAsync(string sessionKey);
    }
}

