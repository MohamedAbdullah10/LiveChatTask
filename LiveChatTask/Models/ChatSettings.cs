using System;

namespace LiveChatTask.Models
{
    /// <summary>
    /// Singleton-style persisted settings for chat behavior (admin configurable).
    /// </summary>
    public class ChatSettings
    {
        public int Id { get; set; }

        /// <summary>
        /// Maximum number of characters allowed for a USER message.
        /// </summary>
        public int MaxUserMessageLength { get; set; } = 500;

        /// <summary>
        /// Maximum duration for a chat session in minutes (admin configurable, default 60 minutes).
        /// </summary>
        public int MaxSessionDurationMinutes { get; set; } = 60;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? UpdatedByAdminId { get; set; }
    }
}

