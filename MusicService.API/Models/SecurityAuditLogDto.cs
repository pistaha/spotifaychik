using System;
using MusicService.Domain.Entities;

namespace MusicService.API.Models
{
    public class SecurityAuditLogDto
    {
        public Guid Id { get; set; }
        public SecurityEventType EventType { get; set; }
        public Guid? UserId { get; set; }
        public string? Email { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool Success { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
