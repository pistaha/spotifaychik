using MediatR;
using System;
using MusicService.Application.Playlists.Dtos;

namespace MusicService.Application.Playlists.Commands
{
    public record UpdatePlaylistCommand : IRequest<PlaylistDto?>
    {
        public Guid PlaylistId { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? CoverImage { get; init; }
        public bool? IsPublic { get; init; }
        public bool? IsCollaborative { get; init; }
        public string? Type { get; init; }
    }
}
