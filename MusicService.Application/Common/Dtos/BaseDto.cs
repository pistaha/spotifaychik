using System;

namespace MusicService.Application.Common.Dtos
{
    public abstract class BaseDto
    {
        /// <summary>идентификатор</summary>
        public Guid Id { get; set; }
        /// <summary>дата создания</summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>дата обновления</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
