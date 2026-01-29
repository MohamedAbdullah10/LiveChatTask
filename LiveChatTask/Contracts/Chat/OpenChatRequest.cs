namespace LiveChatTask.Contracts.Chat
{
    /// <summary>
    /// Web/API DTO: admin request to open (or create) a chat session for a given user.
    /// </summary>
    public class OpenChatRequest
    {
        public string UserId { get; set; } = string.Empty;
    }
}

