using MediatR;
using System.Collections.Generic;
using MusicService.Application.Artists.Dtos;

namespace MusicService.Application.Artists.Queries
{
    public record GetArtistsByGenreQuery : IRequest<List<ArtistDto>>
    {
        public string Genre { get; init; } = string.Empty;
    }
}