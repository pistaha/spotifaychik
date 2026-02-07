using AutoMapper;
using System.Linq;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Artists.Dtos;
using MusicService.Application.Playlists.Dtos;
using MusicService.Application.Tracks.Dtos;
using MusicService.Application.Users.Dtos;
using MusicService.Domain.Entities;

namespace MusicService.Application.Common.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Users mapping
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.PlaylistCount, opt => opt.MapFrom(src => src.CreatedPlaylists.Count))
                .ForMember(dest => dest.FollowingCount, opt => opt.MapFrom(src => src.FollowedArtists.Count + src.FollowedPlaylists.Count))
                .ForMember(dest => dest.FollowerCount, opt => opt.MapFrom(src => src.Friends.Count))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src =>
                    src.UserRoles.Select(ur => ur.Role != null ? ur.Role.Name : string.Empty)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToList()));

            // Artists mapping
            CreateMap<Artist, ArtistDto>()
                .ForMember(dest => dest.AlbumCount, opt => opt.MapFrom(src => src.Albums.Count))
                .ForMember(dest => dest.TrackCount, opt => opt.MapFrom(src => src.Tracks.Count))
                .ForMember(dest => dest.FollowerCount, opt => opt.MapFrom(src => src.Followers.Count));

            // Albums mapping
            CreateMap<Album, AlbumDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.TrackCount, opt => opt.MapFrom(src => src.Tracks.Count))
                .ForMember(dest => dest.ArtistName, opt => opt.MapFrom(src => src.Artist != null ? src.Artist.Name : string.Empty))
                .ForMember(dest => dest.IsRecentRelease, opt => opt.MapFrom(src => src.IsRecentRelease()));

            // Tracks mapping
            CreateMap<Track, TrackDto>()
                .ForMember(dest => dest.DurationFormatted, opt => opt.MapFrom(src => src.DurationFormatted))
                .ForMember(dest => dest.AlbumTitle, opt => opt.MapFrom(src => src.Album != null ? src.Album.Title : string.Empty))
                .ForMember(dest => dest.AlbumCoverImage, opt => opt.MapFrom(src => src.Album != null ? src.Album.CoverImage : null))
                .ForMember(dest => dest.ArtistName, opt => opt.MapFrom(src => src.Artist != null ? src.Artist.Name : string.Empty));

            // Playlists mapping
            CreateMap<Playlist, PlaylistDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.TrackCount, opt => opt.MapFrom(src => src.PlaylistTracks.Count))
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedBy != null ? src.CreatedBy.Username : string.Empty));
        }
    }
}
