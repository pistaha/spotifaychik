using MediatR;
using System;

namespace MusicService.Application.Playlists.Commands
{
    public record DeletePlaylistCommand : IRequest<bool>
    {
        public Guid PlaylistId { get; init; }
    }
}
