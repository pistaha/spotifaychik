using AutoMapper;
using MusicService.Application.Search.Dtos;
using MusicService.Domain.Entities;
using System.Linq;

namespace MusicService.Application.Search.Mapping
{
    public class SearchMappingProfile : Profile
    {
        public SearchMappingProfile()
        {
            // Маппинг для артистов
            CreateMap<Artist, ArtistSearchResultDto>()
                .ForMember(dest => dest.Relevance, opt => opt.Ignore());

            // Маппинг для альбомов
            CreateMap<Album, AlbumSearchResultDto>()
                .ForMember(dest => dest.ArtistName, opt => opt.MapFrom(src => src.Artist != null ? src.Artist.Name : "Unknown"))
                .ForMember(dest => dest.ReleaseYear, opt => opt.MapFrom(src => src.ReleaseDate.Year))
                .ForMember(dest => dest.Relevance, opt => opt.Ignore());

            // Маппинг для треков
            CreateMap<Track, TrackSearchResultDto>()
                .ForMember(dest => dest.ArtistName, opt => opt.MapFrom(src => src.Artist != null ? src.Artist.Name : "Unknown"))
                .ForMember(dest => dest.AlbumTitle, opt => opt.MapFrom(src => src.Album != null ? src.Album.Title : "Unknown"))
                .ForMember(dest => dest.Relevance, opt => opt.Ignore());

            // Маппинг для плейлистов
            CreateMap<Playlist, PlaylistSearchResultDto>()
                .ForMember(dest => dest.CreatorName, opt => opt.MapFrom(src => src.CreatedBy != null ? src.CreatedBy.Username : "Unknown"))
                .ForMember(dest => dest.TrackCount, opt => opt.MapFrom(src => src.PlaylistTracks.Count))
                .ForMember(dest => dest.Relevance, opt => opt.Ignore());

            // Маппинг для пользователей
            CreateMap<User, UserSearchResultDto>()
                .ForMember(dest => dest.Relevance, opt => opt.Ignore());

            // Маппинг для глобального поиска
            CreateMap<Artist, GlobalArtistDto>()
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ProfileImage));

            CreateMap<Album, GlobalAlbumDto>()
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.CoverImage))
                .ForMember(dest => dest.ArtistName, opt => opt.MapFrom(src => src.Artist != null ? src.Artist.Name : "Unknown"));

            CreateMap<Track, GlobalTrackDto>()
                .ForMember(dest => dest.ArtistName, opt => opt.MapFrom(src => src.Artist != null ? src.Artist.Name : "Unknown"))
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.DurationSeconds));

            CreateMap<Playlist, GlobalPlaylistDto>()
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.CoverImage))
                .ForMember(dest => dest.CreatorName, opt => opt.MapFrom(src => src.CreatedBy != null ? src.CreatedBy.Username : "Unknown"));
        }
    }
}