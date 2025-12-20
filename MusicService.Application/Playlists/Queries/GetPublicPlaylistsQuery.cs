using MediatR;
using System.Collections.Generic;
using MusicService.Application.Playlists.Dtos;

namespace MusicService.Application.Playlists.Queries
{
    public record GetPublicPlaylistsQuery : IRequest<List<PlaylistDto>>
    {
        public int? Limit { get; init; }
        public string? SortBy { get; init; } = "FollowersCount";
        public string? SortOrder { get; init; } = "desc";
    }
}