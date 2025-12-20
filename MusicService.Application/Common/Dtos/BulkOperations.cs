using System;
using System.Collections.Generic;

namespace MusicService.Application.Common.Dtos
{
    public class BulkOperationResult<T>
    {
        public List<BulkOperationItem<T>> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int SuccessfulCount { get; set; }
        public int FailedCount { get; set; }
        
        public bool AllSucceeded => FailedCount == 0;
        public double SuccessRate => TotalCount > 0 ? (double)SuccessfulCount / TotalCount * 100 : 0;
    }

    public class BulkOperationItem<T>
    {
        public T? Data { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
        public Guid? ItemId { get; set; }
    }

    public class BulkDeleteResult
    {
        public int TotalCount { get; set; }
        public int SuccessfulCount { get; set; }
        public int FailedCount { get; set; }
        public List<BulkDeleteItem> Items { get; set; } = new();
    }

    public class BulkDeleteItem
    {
        public Guid Id { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
    }
}