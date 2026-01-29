using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LiveChatTask.Services
{
    /// <summary>
    /// Service for handling file uploads (images, documents, voice recordings).
    /// </summary>
    public interface IFileUploadService
    {
        /// <summary>
        /// Uploads a file (image or document) and returns the file path and metadata.
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <returns>File upload result with path, type, name, and size</returns>
        Task<FileUploadResult> UploadFileAsync(IFormFile file);

        /// <summary>
        /// Uploads a voice recording (audio file) and returns the file path and metadata.
        /// </summary>
        /// <param name="file">The audio file to upload</param>
        /// <returns>File upload result with path, type, name, and size</returns>
        Task<FileUploadResult> UploadVoiceAsync(IFormFile file);
    }

    /// <summary>
    /// Result object returned from file upload operations.
    /// </summary>
    public class FileUploadResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }
}
