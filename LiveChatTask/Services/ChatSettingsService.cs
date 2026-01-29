using System;
using System.Threading.Tasks;
using LiveChatTask.Data;
using LiveChatTask.Models;
using Microsoft.EntityFrameworkCore;

namespace LiveChatTask.Services
{
    public class ChatSettingsService : IChatSettingsService
    {
        private readonly AppDbContext _context;

        public ChatSettingsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ChatSettings> GetAsync()
        {
            var settings = await _context.ChatSettings.FirstOrDefaultAsync();
            if (settings != null)
            {
                return settings;
            }

            // Create default row if missing (defensive; DB initializer should seed this).
            settings = new ChatSettings { MaxUserMessageLength = 500, UpdatedAt = DateTime.UtcNow };
            _context.ChatSettings.Add(settings);
            await _context.SaveChangesAsync();
            return settings;
        }

        public async Task<int> GetMaxUserMessageLengthAsync()
        {
            var settings = await GetAsync();
            return settings.MaxUserMessageLength;
        }

        public async Task<ChatSettings> UpdateMaxUserMessageLengthAsync(int maxUserMessageLength, string adminId)
        {
            if (maxUserMessageLength < 10 || maxUserMessageLength > 5000)
            {
                throw new ArgumentException("MaxUserMessageLength must be between 10 and 5000.");
            }

            if (string.IsNullOrWhiteSpace(adminId))
            {
                throw new InvalidOperationException("adminId is required.");
            }

            var settings = await GetAsync();
            settings.MaxUserMessageLength = maxUserMessageLength;
            settings.UpdatedAt = DateTime.UtcNow;
            settings.UpdatedByAdminId = adminId;

            await _context.SaveChangesAsync();
            return settings;
        }
    }
}

