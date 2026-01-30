using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LiveChatTask.Services
{
    public interface IFileUploadService
    {
        Task<FileUploadResult> UploadFileAsync(IFormFile file);
        Task<FileUploadResult> UploadVoiceAsync(IFormFile file);
    }

    public class FileUploadResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }
}
