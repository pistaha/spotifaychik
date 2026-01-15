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
    public class GetPublicPlaylistsQueryHandler : IRequestHandler<GetPublicPlaylistsQuery, List<PlaylistDto>>
    {
        private readonly IMusicServiceDbContext _dbContext;

        public GetPublicPlaylistsQueryHandler(
            IMusicServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<PlaylistDto>> Handle(GetPublicPlaylistsQuery request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Playlists
                .AsNoTracking()
                .Where(p => p.IsPublic);

            query = request.SortBy?.ToLower() switch
            {
                "title" => request.SortOrder == "asc" ?
                    query.OrderBy(p => p.Title) :
                    query.OrderByDescending(p => p.Title),
                "createdat" => request.SortOrder == "asc" ?
                    query.OrderBy(p => p.CreatedAt) :
                    query.OrderByDescending(p => p.CreatedAt),
                "followerscount" => request.SortOrder == "asc" ?
                    query.OrderBy(p => p.FollowersCount) :
                    query.OrderByDescending(p => p.FollowersCount),
                _ => request.SortOrder == "asc" ?
                    query.OrderBy(p => p.Title) :
                    query.OrderByDescending(p => p.Title)
            };

            if (request.Limit.HasValue && request.Limit.Value > 0)
            {
                query = query.Take(request.Limit.Value);
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
