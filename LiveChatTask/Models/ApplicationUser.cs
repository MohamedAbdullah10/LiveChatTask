using Microsoft.AspNetCore.Identity;

namespace LiveChatTask.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Role: "Admin" or "User"
        public string Role { get; set; }

        // SignalR connection
        public string? ConnectionId { get; set; }

        // Online status
        public bool IsOnline { get; set; } = false;

        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        // Navigation: ChatSessions as User
        public ICollection<ChatSession> ChatSessions { get; set; }

        // Navigation: ChatSessions as Admin
        public ICollection<ChatSession> AdminChatSessions { get; set; }

        // Navigation: Messages sent
        public ICollection<Message> MessagesSent { get; set; }
    }
}
