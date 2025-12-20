using MediatR;
using System;
using System.Collections.Generic;
using MusicService.Application.Albums.Dtos;

namespace MusicService.Application.Albums.Commands
{
    public record CreateAlbumCommand : IRequest<AlbumDto>
    {
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? CoverImage { get; init; }
        public DateTime ReleaseDate { get; init; }
        public string Type { get; init; } = string.Empty;
        public List<string> Genres { get; init; } = new();
        public Guid ArtistId { get; init; }
    }
}