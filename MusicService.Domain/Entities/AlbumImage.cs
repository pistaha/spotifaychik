using System;

namespace MusicService.Domain.Entities
{
    public class AlbumImage
    {
        public Guid AlbumId { get; set; }
        public Album? Album { get; set; }

        public Guid FileId { get; set; }
        public FileMetadata? File { get; set; }

        public bool IsMain { get; set; }
        public int Order { get; set; }
        public DateTime AttachedAt { get; set; }
    }
}
