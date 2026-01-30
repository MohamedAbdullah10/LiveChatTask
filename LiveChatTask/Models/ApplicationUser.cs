using Microsoft.AspNetCore.Identity;

namespace LiveChatTask.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Role { get; set; } // "Admin" or "User"
        public string? ConnectionId { get; set; }
        public bool IsOnline { get; set; } = false;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        public ICollection<ChatSession> ChatSessions { get; set; }
        public ICollection<ChatSession> AdminChatSessions { get; set; }
        public ICollection<Message> MessagesSent { get; set; }
    }
}
