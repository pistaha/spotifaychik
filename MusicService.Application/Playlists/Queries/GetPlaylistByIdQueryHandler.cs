using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Common.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Playlists.Queries
{
    public class GetPlaylistByIdQueryHandler : IRequestHandler<GetPlaylistByIdQuery, PlaylistDto?>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetPlaylistByIdQueryHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<PlaylistDto?> Handle(GetPlaylistByIdQuery request, CancellationToken cancellationToken)
        {
            var playlist = await _dbContext.Playlists
                .AsNoTracking()
                .Include(p => p.CreatedBy)
                .Include(p => p.PlaylistTracks)
                .Include(p => p.Followers)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, cancellationToken);
            
            if (playlist == null)
                return null;

            if (!playlist.IsPublic)
            {
                var isOwner = request.UserId.HasValue && playlist.CreatedById == request.UserId.Value;
                if (!request.AllowPrivateAccess && !(request.IncludePrivate && isOwner))
                {
                    return null;
                }
            }

            var dto = _mapper.Map<PlaylistDto>(playlist);
            
            // Проверяем, подписан ли пользователь на плейлист
            if (request.UserId.HasValue && playlist.Followers != null)
            {
                dto.IsFollowing = playlist.Followers.Any(f => f.Id == request.UserId.Value);
            }

            return dto;
        }
    }
}
