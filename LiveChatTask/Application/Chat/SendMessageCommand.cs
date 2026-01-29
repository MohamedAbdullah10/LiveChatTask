using LiveChatTask.Models;

namespace LiveChatTask.Application.Chat
{
    /// <summary>
    /// Application-layer command for sending a chat message.
    /// No web/HTTP types here.
    /// </summary>
    public class SendMessageCommand
    {
        public string ChatSessionId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Admin" or "User"
        public string Content { get; set; } = string.Empty;
        public MessageType MessageType { get; set; } = MessageType.Text;
    }
}

