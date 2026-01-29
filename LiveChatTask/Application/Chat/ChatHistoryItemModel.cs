using LiveChatTask.Models;

namespace LiveChatTask.Application.Chat
{
    /// <summary>
    /// Application-layer model representing a chat history item.
    /// </summary>
    public class ChatHistoryItemModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public bool IsSeen { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public MessageType MessageType { get; set; } = MessageType.Text;
    }
}
