namespace LiveChatTask.Contracts.Chat
{
    /// <summary>
    /// Web/API response DTO for chat history.
    /// </summary>
    public class ChatHistoryItemResponse
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public bool IsSeen { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string MessageType { get; set; } = "Text";
    }
}
