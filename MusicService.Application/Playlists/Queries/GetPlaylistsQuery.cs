using MediatR;
using System;
using System.Collections.Generic;
using MusicService.Application.Playlists.Dtos;

namespace MusicService.Application.Playlists.Queries
{
    public record GetPlaylistsQuery : IRequest<List<PlaylistDto>>
    {
        public Guid? UserId { get; init; }
        public bool IncludePrivate { get; init; } = true;
    }
}
