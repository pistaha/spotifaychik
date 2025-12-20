using MediatR;
using System.Collections.Generic;
using MusicService.Application.Playlists.Dtos;

namespace MusicService.Application.AI.Queries
{
    public record GeneratePersonalPlaylistsQuery : IRequest<List<PlaylistDto>>
    {
        public Guid UserId { get; init; }
        public int Count { get; init; } = 3;
    }
}