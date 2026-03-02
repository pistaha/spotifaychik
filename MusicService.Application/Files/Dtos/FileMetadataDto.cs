using System;

namespace MusicService.Application.Files.Dtos
{
    public sealed class FileMetadataDto
    {
        /// <summary>идентификатор</summary>
        public Guid Id { get; set; }
        /// <summary>оригинальное имя файла</summary>
        public string OriginalFileName { get; set; } = string.Empty;
        /// <summary>размер в байтах</summary>
        public long Size { get; set; }
        /// <summary>тип файла</summary>
        public string ContentType { get; set; } = string.Empty;
        /// <summary>ссылка на файл</summary>
        public string Url { get; set; } = string.Empty;
        /// <summary>ссылка на превью</summary>
        public string? ThumbnailUrl { get; set; }
        /// <summary>ширина</summary>
        public int? Width { get; set; }
        /// <summary>высота</summary>
        public int? Height { get; set; }
        /// <summary>время загрузки</summary>
        public DateTime UploadedAt { get; set; }
    }
}
