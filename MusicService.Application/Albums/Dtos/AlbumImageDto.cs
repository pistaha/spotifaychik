using System;

namespace MusicService.Application.Albums.Dtos
{
    public sealed class AlbumImageDto
    {
        /// <summary>id файла</summary>
        public Guid FileId { get; set; }
        /// <summary>основное изображение</summary>
        public bool IsMain { get; set; }
        /// <summary>порядок</summary>
        public int Order { get; set; }
        /// <summary>ссылка на файл</summary>
        public string FileUrl { get; set; } = string.Empty;
        /// <summary>ссылка на превью</summary>
        public string? ThumbnailUrl { get; set; }
        /// <summary>размер файла</summary>
        public long FileSize { get; set; }
        /// <summary>тип файла</summary>
        public string FileType { get; set; } = string.Empty;
    }
}
