using MediatR;
using System.Collections.Generic;
using MusicService.Application.Artists.Dtos;

namespace MusicService.Application.Artists.Queries
{
    public record GetTopArtistsQuery : IRequest<List<ArtistDto>>
    {
        public int Count { get; init; } = 10;
        public Guid? UserId { get; init; }
    }
}
