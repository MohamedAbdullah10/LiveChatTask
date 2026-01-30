namespace LiveChatTask.Models
{
    public class ChatSession
    {
        public int Id { get; set; }

        public string SessionKey { get; set; } = string.Empty; // Used as SignalR group name

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string? AdminId { get; set; } // Assigned when admin opens chat
        public ApplicationUser? Admin { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public int MaxDurationMinutes { get; set; } = 60;

        public DateTime LastUserMessageAt { get; set; } = DateTime.UtcNow; // For idle timeout
        public DateTime? IdleTerminationSentAt { get; set; } // Set when "chat will terminate" sent

        public ICollection<Message> Messages { get; set; }
    }
}
