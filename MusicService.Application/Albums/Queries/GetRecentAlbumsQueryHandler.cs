using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Albums.Queries
{
    public class GetRecentAlbumsQueryHandler : IRequestHandler<GetRecentAlbumsQuery, List<AlbumDto>>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetRecentAlbumsQueryHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<AlbumDto>> Handle(GetRecentAlbumsQuery request, CancellationToken cancellationToken)
        {
            var threshold = DateTime.UtcNow.AddDays(-request.Days);
            var query = _dbContext.Albums
                .AsNoTracking()
                .Where(a => a.ReleaseDate >= threshold)
                .Include(a => a.Artist)
                .Include(a => a.Tracks)
                .AsSplitQuery()
                .AsQueryable();

            if (request.UserId.HasValue)
            {
                query = query.Where(a => a.CreatedById == request.UserId.Value);
            }

            var albums = await query.ToListAsync(cancellationToken);
            return _mapper.Map<List<AlbumDto>>(albums);
        }
    }
}
