using MediatR;
using System;
using System.Collections.Generic;
using MusicService.Application.Artists.Dtos;

namespace MusicService.Application.Artists.Commands
{
    public record CreateArtistCommand : IRequest<ArtistDto>
    {
        public string Name { get; init; } = string.Empty;
        public string? RealName { get; init; }
        public string? Biography { get; init; }
        public string? ProfileImage { get; init; }
        public string? CoverImage { get; init; }
        public List<string> Genres { get; init; } = new();
        public string Country { get; init; } = "Unknown";
        public DateTime? CareerStartDate { get; init; }
        public Guid CreatedById { get; init; }
    }
}
