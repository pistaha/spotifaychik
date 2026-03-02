using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicService.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Playlists.Commands
{
    public class DeletePlaylistCommandHandler : IRequestHandler<DeletePlaylistCommand, bool>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly ILogger<DeletePlaylistCommandHandler> _logger;

        public DeletePlaylistCommandHandler(
            IMusicServiceDbContext dbContext,
            ILogger<DeletePlaylistCommandHandler> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> Handle(DeletePlaylistCommand request, CancellationToken cancellationToken)
        {
            var playlist = await _dbContext.Playlists
                .FirstOrDefaultAsync(p => p.Id == request.PlaylistId, cancellationToken);
            if (playlist == null)
            {
                return false;
            }

            _dbContext.Playlists.Remove(playlist);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Playlist {PlaylistId} deleted", playlist.Id);
            return true;
        }
    }
}
