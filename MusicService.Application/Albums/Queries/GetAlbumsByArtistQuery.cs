using MediatR;
using System;
using System.Collections.Generic;
using MusicService.Application.Albums.Dtos;

namespace MusicService.Application.Albums.Queries
{
    public record GetAlbumsByArtistQuery : IRequest<List<AlbumDto>>
    {
        public Guid ArtistId { get; init; }
    }
}