using System.Threading.Tasks;
using LiveChatTask.Models;

namespace LiveChatTask.Services
{
    public interface IChatSettingsService
    {
        Task<ChatSettings> GetAsync();
        Task<int> GetMaxUserMessageLengthAsync();
        Task<ChatSettings> UpdateMaxUserMessageLengthAsync(int maxUserMessageLength, string adminId);
    }
}

