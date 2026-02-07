using System;

namespace MusicService.Domain.Entities
{
    public class FileUploadSession
    {
        public Guid Id { get; set; }
        public string UploadId { get; set; } = string.Empty;
        public Guid UploadedBy { get; set; }
        public User? UploadedByUser { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int TotalChunks { get; set; }
        public int UploadedChunks { get; set; }
        public long TotalSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsCompleted { get; set; }
    }
}
