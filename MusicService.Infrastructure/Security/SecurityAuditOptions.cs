namespace MusicService.Infrastructure.Security
{
    public sealed class SecurityAuditOptions
    {
        public int RetentionDays { get; set; } = 90;
        public int CleanupIntervalHours { get; set; } = 24;
    }
}
