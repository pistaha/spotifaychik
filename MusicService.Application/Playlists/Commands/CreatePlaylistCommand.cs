using MediatR;
using MusicService.Application.Playlists.Dtos;

namespace MusicService.Application.Playlists.Commands
{
    public record CreatePlaylistCommand : IRequest<PlaylistDto>
    {
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? CoverImage { get; init; }
        public bool IsPublic { get; init; } = true;
        public bool IsCollaborative { get; init; } = false;
        public string Type { get; init; } = "UserCreated";
        public Guid CreatedBy { get; init; }
    }
}