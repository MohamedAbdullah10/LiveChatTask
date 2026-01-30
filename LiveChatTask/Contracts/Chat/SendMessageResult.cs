namespace LiveChatTask.Contracts.Chat
{
    /// <summary>
    /// Result of sending a message (DTO; no domain entity reference).
    /// </summary>
    public class SendMessageResult
    {
        public int MessageId { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public bool IsSeen { get; set; }
        public string MessageType { get; set; } = "Text";
        public string ChatSessionKey { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        /// <summary>When sender is User, set to session's UserId for admin unread broadcast.</summary>
        public string? SessionUserId { get; set; }
        /// <summary>When sender is User, new unread count for that user (for admin badge).</summary>
        public int? UnreadCountForAdmin { get; set; }
    }
}
