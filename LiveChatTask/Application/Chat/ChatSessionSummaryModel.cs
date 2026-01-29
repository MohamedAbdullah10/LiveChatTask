namespace LiveChatTask.Application.Chat
{
    /// <summary>
    /// Application-layer model representing an admin inbox/session summary.
    /// </summary>
    public class ChatSessionSummaryModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserNameOrEmail { get; set; } = string.Empty;
        public string? ChatSessionId { get; set; }
        public int UnreadCount { get; set; }
        public bool IsOnline { get; set; }
        public System.DateTime LastSeen { get; set; }
    }
}

