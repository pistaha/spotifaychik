using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicService.Application.Playlists.Dtos;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Playlists.Commands
{
    public class CreatePlaylistCommandHandler : IRequestHandler<CreatePlaylistCommand, PlaylistDto>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<CreatePlaylistCommandHandler> _logger;

        public CreatePlaylistCommandHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper,
            ILogger<CreatePlaylistCommandHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PlaylistDto> Handle(CreatePlaylistCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating playlist: {Title}", request.Title);

            var userExists = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.CreatedBy, cancellationToken);
            if (!userExists)
                throw new ArgumentException($"User with ID {request.CreatedBy} not found");

            var playlist = new Playlist
            {
                Title = request.Title,
                Description = request.Description,
                CoverImage = request.CoverImage,
                IsPublic = request.IsPublic,
                IsCollaborative = request.IsCollaborative,
                Type = Enum.Parse<PlaylistType>(request.Type),
                CreatedById = request.CreatedBy,
                FollowersCount = 0,
                TotalDurationMinutes = 0
            };

            _dbContext.Playlists.Add(playlist);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Playlist {PlaylistId} created successfully", playlist.Id);
            
            return _mapper.Map<PlaylistDto>(playlist);
        }
    }
}
