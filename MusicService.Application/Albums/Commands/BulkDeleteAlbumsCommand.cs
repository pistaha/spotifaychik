using MediatR;
using System;
using System.Collections.Generic;
using MusicService.Application.Common.Dtos;

namespace MusicService.Application.Albums.Commands
{
    public record BulkDeleteAlbumsCommand : IRequest<BulkDeleteResult>
    {
        public List<Guid> AlbumIds { get; init; } = new();
    }
}