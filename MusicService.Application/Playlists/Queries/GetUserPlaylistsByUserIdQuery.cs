using System;
using System.Collections.Generic;
using MediatR;
using MusicService.Application.Playlists.Dtos;

namespace MusicService.Application.Playlists.Queries;

public record GetUserPlaylistsByUserIdQuery : IRequest<List<PlaylistDto>>
{
    public Guid UserId { get; init; }
}
