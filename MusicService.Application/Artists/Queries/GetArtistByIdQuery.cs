using MediatR;
using MusicService.Application.Artists.Dtos;

namespace MusicService.Application.Artists.Queries
{
    public record GetArtistByIdQuery : IRequest<ArtistDto?>
    {
        public Guid ArtistId { get; init; }
        public Guid? UserId { get; init; }
    }
}
