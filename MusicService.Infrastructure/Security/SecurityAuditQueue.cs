using System.Threading.Channels;
using MusicService.Application.Common.Interfaces;

namespace MusicService.Infrastructure.Security
{
    public sealed class SecurityAuditQueue
    {
        public SecurityAuditQueue()
        {
            Channel = System.Threading.Channels.Channel.CreateUnbounded<SecurityAuditEntry>();
        }

        public Channel<SecurityAuditEntry> Channel { get; }
    }
}
