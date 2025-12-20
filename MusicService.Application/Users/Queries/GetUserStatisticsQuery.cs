using MediatR;
using System;

namespace MusicService.Application.Users.Queries
{
    public record GetUserStatisticsQuery : IRequest<UserStatisticsDto>
    {
        public Guid UserId { get; init; }
        public int? TimeRangeDays { get; init; } // null = all time
    }

    public class UserStatisticsDto
    {
        public int TotalPlaylists { get; set; }
        public int TotalTracks { get; set; }
        public int TotalListeningTime { get; set; } // в минутах
        public int FollowingCount { get; set; }
        public int FollowersCount { get; set; }
        public int FavoriteGenresCount { get; set; }
        public List<string> TopGenres { get; set; } = new();
        public List<ArtistStatisticsDto> TopArtists { get; set; } = new();
        public List<TrackStatisticsDto> TopTracks { get; set; } = new();
        public DateTime? FirstListenDate { get; set; }
        public DateTime? LastListenDate { get; set; }
    }

    public class ArtistStatisticsDto
    {
        public Guid ArtistId { get; set; }
        public string ArtistName { get; set; } = string.Empty;
        public int ListenCount { get; set; }
        public int TotalDuration { get; set; } // в минутах
    }

    public class TrackStatisticsDto
    {
        public Guid TrackId { get; set; }
        public string TrackTitle { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public int ListenCount { get; set; }
        public DateTime? LastListenDate { get; set; }
    }
}