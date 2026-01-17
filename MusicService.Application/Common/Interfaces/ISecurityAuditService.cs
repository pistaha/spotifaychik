using System;
using System.Threading;
using System.Threading.Tasks;
using MusicService.Domain.Entities;

namespace MusicService.Application.Common.Interfaces
{
    public sealed record SecurityAuditEntry(
        SecurityEventType EventType,
        Guid? UserId,
        string? Email,
        string? IpAddress,
        string? UserAgent,
        bool Success,
        string? Details,
        DateTime Timestamp);

    public interface ISecurityAuditService
    {
        Task EnqueueAsync(SecurityAuditEntry entry, CancellationToken cancellationToken = default);
    }
}
