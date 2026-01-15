using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Artists.Queries
{
    public class GetTopArtistsQueryHandler : IRequestHandler<GetTopArtistsQuery, List<ArtistDto>>
    {
        private readonly IMusicServiceDbContext _dbContext;

        public GetTopArtistsQueryHandler(
            IMusicServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ArtistDto>> Handle(GetTopArtistsQuery request, CancellationToken cancellationToken)
        {
            var nowYear = DateTime.UtcNow.Year;
            return await _dbContext.Artists
                .AsNoTracking()
                .OrderByDescending(a => a.MonthlyListeners)
                .Take(request.Count)
                .Select(a => new ArtistDto
                {
                    Id = a.Id,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt,
                    Name = a.Name,
                    RealName = a.RealName,
                    Biography = a.Biography,
                    ProfileImage = a.ProfileImage,
                    CoverImage = a.CoverImage,
                    Genres = a.Genres,
                    Country = a.Country,
                    IsVerified = a.IsVerified,
                    MonthlyListeners = a.MonthlyListeners,
                    AlbumCount = a.Albums.Count,
                    TrackCount = a.Tracks.Count,
                    FollowerCount = a.Followers.Count,
                    YearsActive = a.CareerStartDate.HasValue ? nowYear - a.CareerStartDate.Value.Year : 0
                })
                .ToListAsync(cancellationToken);
        }
    }
}
