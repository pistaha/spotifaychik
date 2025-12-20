using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces.Repositories;

namespace MusicService.Application.Albums.Queries
{
    public class GetAlbumsQueryHandler : IRequestHandler<GetAlbumsQuery, PagedResult<AlbumDto>>
    {
        private readonly IAlbumRepository _albumRepository;
        private readonly IMapper _mapper;

        public GetAlbumsQueryHandler(IAlbumRepository albumRepository, IMapper mapper)
        {
            _albumRepository = albumRepository;
            _mapper = mapper;
        }

        public async Task<PagedResult<AlbumDto>> Handle(GetAlbumsQuery request, CancellationToken cancellationToken)
        {
            var albums = await _albumRepository.GetAllAsync(cancellationToken);
            IEnumerable<Domain.Entities.Album> query = albums;

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(a =>
                    a.Title.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(a.Description) && a.Description.Contains(request.Search, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(request.Genre))
            {
                query = query.Where(a =>
                    a.Genres.Any(g => g.Equals(request.Genre, StringComparison.OrdinalIgnoreCase)));
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

            var totalCount = query.Count();
            var items = query
                .Skip(Math.Max(0, (request.Page - 1) * request.PageSize))
                .Take(request.PageSize)
                .ToList();

            var dtoItems = _mapper.Map<List<AlbumDto>>(items);
            return new PagedResult<AlbumDto>(dtoItems, totalCount, request.Page, request.PageSize);
        }
    }
}
