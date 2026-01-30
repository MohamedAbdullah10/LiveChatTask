using System.Collections.Generic;
using System.Threading.Tasks;
using LiveChatTask.Contracts.Chat;
using LiveChatTask.Models;

namespace LiveChatTask.Services
{
    public interface IChatService
    {
        Task<ChatSession> GetOrCreateSessionAsync(string userId, string adminId);

        Task<ChatSession> GetOrCreateUserSessionAsync(string userId);

        Task<IReadOnlyList<ChatSessionSummaryResponse>> GetAdminSessionsAsync(string adminId);

        Task<SendMessageResult> SendMessageAsync(SendMessageCommand command, int maxMessageLength);

        Task<IReadOnlyList<ChatHistoryItemResponse>> GetHistoryAsync(string requesterId, string requesterRole, string chatSessionKey);

        Task<IReadOnlyList<int>> MarkMessagesAsSeenAsync(string chatSessionKey, string viewerId, string viewerRole);

        Task<ChatSession?> GetSessionInfoAsync(string chatSessionKey, string requesterId, string requesterRole);

        Task<IReadOnlyList<string>> GetSessionKeysForIdleTerminationAsync();

        Task<IdleTerminationResult?> SendIdleTerminationIfNeededAsync(string sessionKey);
    }
}

