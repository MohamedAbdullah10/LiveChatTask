using System;

namespace LiveChatTask.Models
{
    public enum MessageType { Text, Image, File, Voice, System }

    public class Message
    {
        public int Id { get; set; }

        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        public int ChatSessionId { get; set; }
        public ChatSession ChatSession { get; set; }

        public string Content { get; set; } // Text or file path
        public MessageType Type { get; set; }
        public bool IsSeen { get; set; } = false; // Read receipt
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
