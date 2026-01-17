using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces;

namespace MusicService.Application.Albums.Queries
{
    public class GetAlbumsQueryHandler : IRequestHandler<GetAlbumsQuery, PagedResult<AlbumDto>>
    {
        private readonly IMusicServiceDbContext _dbContext;

        public GetAlbumsQueryHandler(IMusicServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PagedResult<AlbumDto>> Handle(GetAlbumsQuery request, CancellationToken cancellationToken)
        {
            var recentThreshold = DateTime.UtcNow.AddDays(-30);
            IQueryable<Domain.Entities.Album> query = _dbContext.Albums.AsNoTracking();

            if (request.UserId.HasValue)
            {
                query = query.Where(a => a.CreatedById == request.UserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(a =>
                    EF.Functions.ILike(a.Title, $"%{search}%") ||
                    (a.Description != null && EF.Functions.ILike(a.Description, $"%{search}%")));
            }

            if (!string.IsNullOrWhiteSpace(request.Genre))
            {
                var genre = request.Genre.Trim();
                query = query.Where(a => a.Genres.Any(g => g == genre));
            }

            query = (request.SortBy?.ToLowerInvariant()) switch
            {
                "title" => request.SortOrder?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true
                    ? query.OrderBy(a => a.Title)
                    : query.OrderByDescending(a => a.Title),
                "releasedate" => request.SortOrder?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true
                    ? query.OrderBy(a => a.ReleaseDate)
                    : query.OrderByDescending(a => a.ReleaseDate),
                _ => request.SortOrder?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true
                    ? query.OrderBy(a => a.CreatedAt)
                    : query.OrderByDescending(a => a.CreatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var rawItems = await query
                .Skip(Math.Max(0, (request.Page - 1) * request.PageSize))
                .Take(request.PageSize)
                .Select(a => new
                {
                    a.Id,
                    a.CreatedAt,
                    a.UpdatedAt,
                    a.Title,
                    a.Description,
                    a.CoverImage,
                    a.ReleaseDate,
                    a.Type,
                    a.Genres,
                    a.TotalDurationMinutes,
                    TrackCount = a.Tracks.Count,
                    a.ArtistId,
                    ArtistName = a.Artist != null ? a.Artist.Name : string.Empty
                })
                .ToListAsync(cancellationToken);

            var items = rawItems.Select(a => new AlbumDto
            {
                Id = a.Id,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                Title = a.Title,
                Description = a.Description,
                CoverImage = a.CoverImage,
                ReleaseDate = a.ReleaseDate,
                Type = a.Type.ToString(),
                Genres = a.Genres,
                TotalDurationMinutes = a.TotalDurationMinutes,
                TrackCount = a.TrackCount,
                ArtistId = a.ArtistId,
                ArtistName = a.ArtistName,
                Tracks = new List<MusicService.Application.Tracks.Dtos.TrackDto>(),
                IsRecentRelease = a.ReleaseDate >= recentThreshold
            }).ToList();

            return new PagedResult<AlbumDto>(items, totalCount, request.Page, request.PageSize);
        }
    }
}
