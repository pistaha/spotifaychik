using System;

namespace MusicService.Domain.Entities
{
    public class UserRole
    {
        public Guid UserId { get; set; }
        public User? User { get; set; }

        public Guid RoleId { get; set; }
        public Role? Role { get; set; }

        public DateTime AssignedAt { get; set; }
        public Guid? AssignedBy { get; set; }
    }
}
