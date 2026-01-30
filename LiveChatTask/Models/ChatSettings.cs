using System;

namespace LiveChatTask.Models
{
    // Admin-configurable chat limits (single row in DB)
    public class ChatSettings
    {
        public int Id { get; set; }

        public int MaxUserMessageLength { get; set; } = 500;
        public int MaxSessionDurationMinutes { get; set; } = 60;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedByAdminId { get; set; }
    }
}

