using LiveChatTask.Models;

namespace LiveChatTask.Application.Chat
{
    public class SendMessageResult
    {
        public Message Message { get; set; } = new Message();
        public string ChatSessionKey { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        /// <summary>When sender is User, set to session's UserId for admin unread broadcast.</summary>
        public string? SessionUserId { get; set; }
        /// <summary>When sender is User, new unread count for that user (for admin badge).</summary>
        public int? UnreadCountForAdmin { get; set; }
    }
}

