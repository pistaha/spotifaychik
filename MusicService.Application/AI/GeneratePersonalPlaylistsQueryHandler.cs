using MediatR;
using MusicService.Application.Playlists.Dtos;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.AI.Queries
{
    public class GeneratePersonalPlaylistsQueryHandler : IRequestHandler<GeneratePersonalPlaylistsQuery, List<PlaylistDto>>
    {
        private readonly IUserRepository _userRepository;

        public GeneratePersonalPlaylistsQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<PlaylistDto>> Handle(GeneratePersonalPlaylistsQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
                return new List<PlaylistDto>();

            var playlists = new List<PlaylistDto>();
            var random = new Random();

            // Генерация персональных плейлистов на основе интересов пользователя
            foreach (var genre in user.FavoriteGenres.Take(request.Count))
            {
                var playlist = GeneratePersonalPlaylist(genre, user.Id, random);
                playlists.Add(playlist);
            }

            return playlists;
        }

        private PlaylistDto GeneratePersonalPlaylist(string genre, Guid userId, Random random)
        {
            var title = genre switch
            {
                "Rock" => "Rock Classics Mix",
                "Pop" => "Top Pop Hits",
                "Hip-Hop" => "Hip-Hop Essentials",
                "Jazz" => "Smooth Jazz Collection",
                "Classical" => "Classical Masterpieces",
                _ => $"My {genre} Mix"
            };

            var description = genre switch
            {
                "Rock" => "The best rock tracks from different eras",
                "Pop" => "Current pop hits and timeless classics",
                "Hip-Hop" => "Essential hip-hop tracks for any mood",
                "Jazz" => "Relaxing jazz for work or study",
                "Classical" => "Timeless classical compositions",
                _ => $"Personalized {genre} playlist based on your listening habits"
            };

            return new PlaylistDto
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                Type = "SystemGenerated",
                IsPublic = true,
                IsCollaborative = false,
                FollowersCount = random.Next(100, 10000),
                TotalDurationMinutes = random.Next(60, 180),
                TrackCount = random.Next(15, 40),
                CreatedById = userId,
                CreatedByName = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}