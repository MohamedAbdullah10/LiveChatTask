namespace LiveChatTask.Models
{
    public class ChatSession
    {
        public int Id { get; set; }

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

        // Last message sent by user (for timeout logic)
        public DateTime LastUserMessageAt { get; set; } = DateTime.UtcNow;

        // Messages in this session
        public ICollection<Message> Messages { get; set; }
    }
}
