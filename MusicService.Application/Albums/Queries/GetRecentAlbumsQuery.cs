using MediatR;
using System.Collections.Generic;
using MusicService.Application.Albums.Dtos;

namespace MusicService.Application.Albums.Queries
{
    public record GetRecentAlbumsQuery : IRequest<List<AlbumDto>>
    {
        public int Days { get; init; } = 30;
    }
}