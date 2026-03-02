using System;

namespace MusicService.Domain.Entities
{
    public enum SecurityEventType
    {
        Login = 1,
        FailedLogin = 2,
        Logout = 3,
        TokenRefresh = 4,
        Register = 5,
        EmailConfirmed = 6,
        PasswordResetRequested = 7,
        PasswordReset = 8,
        PasswordChanged = 9,
        UserCreated = 10,
        UserUpdated = 11,
        UserDeleted = 12,
        UserBlocked = 13,
        UserUnblocked = 14,
        RoleAssigned = 15,
        RoleRevoked = 16,
        SuspiciousActivity = 17,
        ExpiredTokenUsed = 18,
        ResourceAccessDenied = 19,
        UnusualIpAddress = 20,
        FileDownloaded = 21
    }

    public class SecurityAuditLog
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
