using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicService.Domain.Entities
{
    public class Artist : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? RealName { get; set; }
        public string? Biography { get; set; }
        public string? ProfileImage { get; set; }
        public string? CoverImage { get; set; }
        public List<string> Genres { get; set; } = new();
        public string Country { get; set; } = "Unknown";
        public DateTime? CareerStartDate { get; set; }
        public bool IsVerified { get; set; }
        public int MonthlyListeners { get; set; }
        
        // Навигационные свойства
        public Guid? CreatedById { get; set; }
        public User? CreatedBy { get; set; }
        public List<Album> Albums { get; set; } = new();
        public List<Track> Tracks { get; set; } = new();
        public List<User> Followers { get; set; } = new();

        public int YearsActive
        {
            get
            {
                if (!CareerStartDate.HasValue) return 0;
                return DateTime.UtcNow.Year - CareerStartDate.Value.Year;
            }
        }

        public bool HasGenre(string genre)
        {
            return Genres.Any(g => g.Equals(genre, StringComparison.OrdinalIgnoreCase));
        }
    }
}
