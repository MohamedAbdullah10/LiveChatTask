namespace LiveChatTask.Contracts.Chat
{
    /// <summary>
    /// Command for sending a chat message (used by service layer).
    /// </summary>
    public class SendMessageCommand
    {
        public string ChatSessionId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Admin" or "User"
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "Text";
    }
}
