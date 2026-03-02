using System;

namespace MusicService.API.Models
{
    public sealed class AlbumImageRequest
    {
        /// <summary>id файла</summary>
        public Guid FileId { get; set; }
        /// <summary>основное изображение</summary>
        public bool IsMain { get; set; }
        /// <summary>порядок</summary>
        public int Order { get; set; }
    }
}
