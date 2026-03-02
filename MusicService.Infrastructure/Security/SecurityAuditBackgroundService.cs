using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicService.Application.Common.Interfaces;
using MusicService.Domain.Entities;
using MusicService.Infrastructure.Persistence;

namespace MusicService.Infrastructure.Security
{
    public sealed class SecurityAuditBackgroundService : BackgroundService
    {
        private readonly SecurityAuditQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SecurityAuditBackgroundService> _logger;
        private readonly SecurityAuditOptions _options;

        public SecurityAuditBackgroundService(
            SecurityAuditQueue queue,
            IServiceScopeFactory scopeFactory,
            IOptions<SecurityAuditOptions> options,
            ILogger<SecurityAuditBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cleanupTask = RunCleanupLoopAsync(stoppingToken);

            await foreach (var entry in _queue.Channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<MusicServiceDbContext>();
                    dbContext.SecurityAuditLogs.Add(new SecurityAuditLog
                    {
                        Id = Guid.NewGuid(),
                        EventType = entry.EventType,
                        UserId = entry.UserId,
                        Email = entry.Email,
                        IpAddress = entry.IpAddress,
                        UserAgent = entry.UserAgent,
                        Success = entry.Success,
                        Details = entry.Details,
                        Timestamp = entry.Timestamp
                    });
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to persist security audit log.");
                }
            }

            await cleanupTask;
        }

        private async Task RunCleanupLoopAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromHours(Math.Max(1, _options.CleanupIntervalHours));
            using var timer = new PeriodicTimer(interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, _options.RetentionDays));
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<MusicServiceDbContext>();
                    var oldLogs = await dbContext.SecurityAuditLogs
                        .Where(x => x.Timestamp < cutoff)
                        .ToListAsync(stoppingToken);

                    if (oldLogs.Count > 0)
                    {
                        dbContext.SecurityAuditLogs.RemoveRange(oldLogs);
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cleanup security audit logs.");
                }
            }
        }
    }
}
