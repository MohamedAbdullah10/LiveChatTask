namespace LiveChatTask.Contracts.Chat
{
    /// <summary>
    /// Web/API DTO: request body for sending a message.
    /// </summary>
    public class SendMessageRequest
    {
        public string ChatSessionId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string MessageType { get; set; } = "Text";
    }
}

