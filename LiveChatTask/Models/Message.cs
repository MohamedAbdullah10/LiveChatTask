using System;

namespace LiveChatTask.Models
{
    public enum MessageType
    {
        Text,
        Image,
        File,
        Voice,
        System
    }

    public class Message
    {
        public int Id { get; set; }

        // Sender of the message
        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        // ChatSession the message belongs to
        public int ChatSessionId { get; set; }
        public ChatSession ChatSession { get; set; }

        // Content: text or file path
        public string Content { get; set; }

        public MessageType Type { get; set; }

        // Seen flag
        public bool IsSeen { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
