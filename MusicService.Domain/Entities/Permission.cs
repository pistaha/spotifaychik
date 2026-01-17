using System.Collections.Generic;

namespace MusicService.Domain.Entities
{
    public class Permission : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;

        public List<RolePermission> RolePermissions { get; set; } = new();
    }
}
