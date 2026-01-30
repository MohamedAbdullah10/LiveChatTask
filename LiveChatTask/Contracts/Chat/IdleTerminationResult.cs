namespace LiveChatTask.Contracts.Chat
{
    /// <summary>
    /// Result of sending an idle termination message; used by IdleChatMonitor to broadcast via SignalR.
    /// </summary>
    public class IdleTerminationResult
    {
        public string SessionKey { get; set; } = string.Empty;
        public int MessageId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string SenderId { get; set; } = string.Empty;
    }
}
