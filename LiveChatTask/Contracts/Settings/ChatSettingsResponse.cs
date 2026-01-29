using System;

namespace LiveChatTask.Contracts.Settings
{
    public class ChatSettingsResponse
    {
        public int MaxUserMessageLength { get; set; }
        public int MaxSessionDurationMinutes { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

