namespace LiveChatTask.Contracts.Settings
{
    public class UpdateChatSettingsRequest
    {
        public int? MaxUserMessageLength { get; set; }
        public int? MaxSessionDurationMinutes { get; set; }
    }
}

