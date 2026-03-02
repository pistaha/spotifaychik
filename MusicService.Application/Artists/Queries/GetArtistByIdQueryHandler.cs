using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Common.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Artists.Queries
{
    public class GetArtistByIdQueryHandler : IRequestHandler<GetArtistByIdQuery, ArtistDto?>
    {
        private readonly IMusicServiceDbContext _dbContext;

        public GetArtistByIdQueryHandler(
            IMusicServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ArtistDto?> Handle(GetArtistByIdQuery request, CancellationToken cancellationToken)
        {
            var nowYear = DateTime.UtcNow.Year;
            var query = _dbContext.Artists
                .AsNoTracking()
                .Where(a => a.Id == request.ArtistId);

            if (request.UserId.HasValue)
            {
                query = query.Where(a => a.CreatedById == request.UserId.Value);
            }

            return await query
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
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
