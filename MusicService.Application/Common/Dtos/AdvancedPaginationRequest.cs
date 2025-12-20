using System;

namespace MusicService.Application.Common.Dtos
{
    public class AdvancedPaginationRequest : PaginationRequest
    {
        public string? Search { get; set; }
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortOrder { get; set; } = "desc";
        
        // Расширенные фильтры для дат
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        
        // Множественный выбор
        public string[]? Categories { get; set; }
        public string[]? Priorities { get; set; }
        
        // Дополнительные фильтры
        public bool? IsCompleted { get; set; }
        public bool? IsExpired { get; set; }
        public bool? IsOnTrack { get; set; }
        public int? MinProgress { get; set; }
        public int? MaxProgress { get; set; }
    }
}