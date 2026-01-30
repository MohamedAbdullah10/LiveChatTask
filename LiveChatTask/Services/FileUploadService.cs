using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LiveChatTask.Services
{
    // Handles file uploads for chat: images, documents, voice recordings
    public class FileUploadService : IFileUploadService
    {
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
        private const long MaxVoiceSize = 5 * 1024 * 1024; // 5MB

        public async Task<FileUploadResult> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided", nameof(file));

            if (file.Length > MaxFileSize)
                throw new ArgumentException("File size exceeds 10MB limit");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var allowedDocExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };

            string fileType;
            string subfolder;

            if (allowedImageExtensions.Contains(extension))
            {
                fileType = "image";
                subfolder = "images";
            }
            else if (allowedDocExtensions.Contains(extension))
            {
                fileType = "document";
                subfolder = "documents";
            }
            else
            {
                throw new ArgumentException("File type not allowed. Allowed: images (jpg, png, gif, webp) and documents (pdf, docx, txt)");
            }

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", subfolder);
            Directory.CreateDirectory(uploadsPath);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return new FileUploadResult
            {
                FilePath = $"/uploads/{subfolder}/{uniqueFileName}",
                FileType = fileType,
                FileName = file.FileName,
                FileSize = file.Length
            };
        }

        public async Task<FileUploadResult> UploadVoiceAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No audio file provided", nameof(file));

            if (file.Length > MaxVoiceSize)
                throw new ArgumentException("Voice recording exceeds 5MB limit");

            var allowedMimeTypes = new[] { "audio/webm", "audio/ogg", "audio/mp4", "audio/wav", "audio/mpeg", "audio/x-m4a" };
            var contentType = file.ContentType?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(contentType) || !allowedMimeTypes.Any(mt => contentType.Contains(mt)))
                throw new ArgumentException("Invalid audio file type. Allowed: webm, ogg, mp4, wav, mpeg, m4a");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".webm"; // Browser recorders often omit extension

            var voicesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "voices");
            Directory.CreateDirectory(voicesPath);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(voicesPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return new FileUploadResult
            {
                FilePath = $"/voices/{uniqueFileName}",
                FileType = "voice",
                FileName = file.FileName,
                FileSize = file.Length
            };
        }
    }
}
