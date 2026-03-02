using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Playlists.Dtos;
using MusicService.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Playlists.Commands
{
    public class UpdatePlaylistCommandHandler : IRequestHandler<UpdatePlaylistCommand, PlaylistDto?>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdatePlaylistCommandHandler> _logger;

        public UpdatePlaylistCommandHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper,
            ILogger<UpdatePlaylistCommandHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PlaylistDto?> Handle(UpdatePlaylistCommand request, CancellationToken cancellationToken)
        {
            var playlist = await _dbContext.Playlists
                .Include(p => p.PlaylistTracks)
                .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, cancellationToken);

            if (playlist == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                playlist.Title = request.Title;
            }

            if (request.Description != null)
            {
                playlist.Description = request.Description;
            }

            if (request.CoverImage != null)
            {
                playlist.CoverImage = request.CoverImage;
            }

            if (request.IsPublic.HasValue)
            {
                playlist.IsPublic = request.IsPublic.Value;
            }

            if (request.IsCollaborative.HasValue)
            {
                playlist.IsCollaborative = request.IsCollaborative.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Type) &&
                Enum.TryParse<PlaylistType>(request.Type, true, out var type))
            {
                playlist.Type = type;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Playlist {PlaylistId} updated", playlist.Id);
            return _mapper.Map<PlaylistDto>(playlist);
        }
    }
}
