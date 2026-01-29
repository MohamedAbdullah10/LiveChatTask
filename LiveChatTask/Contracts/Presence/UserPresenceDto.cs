namespace LiveChatTask.Contracts.Presence
{
    public class UserPresenceDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserNameOrEmail { get; set; } = string.Empty;
        public string Status { get; set; } = "Offline"; // Online / Idle / Offline
        public System.DateTime LastSeen { get; set; }
    }
}

