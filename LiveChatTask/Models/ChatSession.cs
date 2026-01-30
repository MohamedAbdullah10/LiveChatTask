namespace LiveChatTask.Models
{
    public class ChatSession
    {
        public int Id { get; set; }

        // Stable key used by the client as chatSessionId and by SignalR as the group name
        public string SessionKey { get; set; } = string.Empty;

        // The User (client) in this chat
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // The Admin in this chat (nullable initially)
        public string? AdminId { get; set; }
        public ApplicationUser? Admin { get; set; }

        // Active / closed chat
        public bool IsActive { get; set; } = true;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Session start time (when the session actually started, defaults to CreatedAt)
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        // Maximum duration for this session in minutes (populated from ChatSettings when session is created)
        public int MaxDurationMinutes { get; set; } = 60;

        // Last message sent by user (for timeout logic)
        public DateTime LastUserMessageAt { get; set; } = DateTime.UtcNow;

        // When the idle termination message was sent (null until sent)
        public DateTime? IdleTerminationSentAt { get; set; }

        // Messages in this session
        public ICollection<Message> Messages { get; set; }
    }
}
