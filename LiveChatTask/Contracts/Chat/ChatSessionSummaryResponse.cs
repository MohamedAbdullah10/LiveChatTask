namespace LiveChatTask.Contracts.Chat
{
    /// <summary>
    /// Web/API response DTO for admin inbox/session summaries.
    /// </summary>
    public class ChatSessionSummaryResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string UserNameOrEmail { get; set; } = string.Empty;
        public string? ChatSessionId { get; set; }
        public int UnreadCount { get; set; }
        public bool IsOnline { get; set; }
        public System.DateTime LastSeen { get; set; }
    }
}

