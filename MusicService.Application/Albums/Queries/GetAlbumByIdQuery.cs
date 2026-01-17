using MediatR;
using MusicService.Application.Albums.Dtos;

namespace MusicService.Application.Albums.Queries
{
    public record GetAlbumByIdQuery : IRequest<AlbumDto?>
    {
        public Guid AlbumId { get; init; }
        public Guid? UserId { get; init; }
    }
}
