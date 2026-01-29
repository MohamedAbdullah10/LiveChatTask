namespace LiveChatTask.Contracts.Chat
{
    public class ChatSessionInfoResponse
    {
        public string ChatSessionId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public int MaxDurationMinutes { get; set; }
        public double RemainingMinutes { get; set; }
        public bool IsExpired { get; set; }
    }
}
