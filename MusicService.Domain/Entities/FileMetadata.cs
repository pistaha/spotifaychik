using System;

namespace MusicService.Domain.Entities
{
    public class FileMetadata
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public Guid UploadedBy { get; set; }
        public User? UploadedByUser { get; set; }
        public DateTime UploadedAt { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int DownloadCount { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? ThumbnailSmallPath { get; set; }
        public string? ThumbnailMediumPath { get; set; }
    }
}
