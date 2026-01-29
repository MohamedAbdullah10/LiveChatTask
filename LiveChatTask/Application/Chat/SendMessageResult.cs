using LiveChatTask.Models;

namespace LiveChatTask.Application.Chat
{
    public class SendMessageResult
    {
        public Message Message { get; set; } = new Message();
        public string ChatSessionKey { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}

