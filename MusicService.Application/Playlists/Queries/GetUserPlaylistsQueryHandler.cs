using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Playlists.Queries
{
    public class GetUserPlaylistsQueryHandler : IRequestHandler<GetUserPlaylistsQuery, List<PlaylistDto>>
    {
        private readonly IMusicServiceDbContext _dbContext;

        public GetUserPlaylistsQueryHandler(
            IMusicServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<PlaylistDto>> Handle(GetUserPlaylistsQuery request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Playlists
                .AsNoTracking()
                .Where(p => p.CreatedById == request.UserId);
            
            if (!request.IncludePrivate)
            {
                query = query.Where(p => p.IsPublic);
            }

            var rawItems = await query
                .Select(p => new
                {
                    p.Id,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.Title,
                    p.Description,
                    p.CoverImage,
                    p.IsPublic,
                    p.IsCollaborative,
                    p.Type,
                    p.FollowersCount,
                    p.TotalDurationMinutes,
                    TrackCount = p.PlaylistTracks.Count,
                    p.CreatedById,
                    CreatedByName = p.CreatedBy != null ? p.CreatedBy.Username : string.Empty
                })
                .ToListAsync(cancellationToken);

            return rawItems.Select(p => new PlaylistDto
            {
                Id = p.Id,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Title = p.Title,
                Description = p.Description,
                CoverImage = p.CoverImage,
                IsPublic = p.IsPublic,
                IsCollaborative = p.IsCollaborative,
                Type = p.Type.ToString(),
                FollowersCount = p.FollowersCount,
                TotalDurationMinutes = p.TotalDurationMinutes,
                TrackCount = p.TrackCount,
                CreatedById = p.CreatedById,
                CreatedByName = p.CreatedByName
            }).ToList();
        }
    }
}
