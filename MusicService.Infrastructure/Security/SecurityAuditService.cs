using System.Threading;
using System.Threading.Tasks;
using MusicService.Application.Common.Interfaces;

namespace MusicService.Infrastructure.Security
{
    public sealed class SecurityAuditService : ISecurityAuditService
    {
        private readonly SecurityAuditQueue _queue;

        public SecurityAuditService(SecurityAuditQueue queue)
        {
            _queue = queue;
        }

        public Task EnqueueAsync(SecurityAuditEntry entry, CancellationToken cancellationToken = default)
        {
            _queue.Channel.Writer.TryWrite(entry);
            return Task.CompletedTask;
        }
    }
}
