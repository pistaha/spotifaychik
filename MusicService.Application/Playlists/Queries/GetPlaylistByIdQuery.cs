using MediatR;
using System;
using MusicService.Application.Playlists.Dtos;

namespace MusicService.Application.Playlists.Queries
{
    public record GetPlaylistByIdQuery : IRequest<PlaylistDto?>
    {
        public Guid PlaylistId { get; init; }
        public Guid? UserId { get; init; }
    }
}