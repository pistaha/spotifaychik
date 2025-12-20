using MediatR;
using System.Collections.Generic;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Albums.Dtos;

namespace MusicService.Application.Albums.Commands
{
    public record BulkCreateAlbumsCommand : IRequest<BulkOperationResult<AlbumDto>>
    {
        public List<CreateAlbumCommand> Commands { get; init; } = new();
    }
}
