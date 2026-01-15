using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicService.Application.Tracks.Dtos;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Tracks.Commands
{
    public class CreateTrackCommandHandler : IRequestHandler<CreateTrackCommand, TrackDto>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateTrackCommandHandler> _logger;

        public CreateTrackCommandHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper,
            ILogger<CreateTrackCommandHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<TrackDto> Handle(CreateTrackCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating track: {Title}", request.Title);

            var albumExists = await _dbContext.Albums
                .AsNoTracking()
                .AnyAsync(a => a.Id == request.AlbumId, cancellationToken);
            if (!albumExists)
                throw new ArgumentException($"Album with ID {request.AlbumId} not found");

            var artistExists = await _dbContext.Artists
                .AsNoTracking()
                .AnyAsync(a => a.Id == request.ArtistId, cancellationToken);
            if (!artistExists)
                throw new ArgumentException($"Artist with ID {request.ArtistId} not found");

            var track = new Track
            {
                Title = request.Title,
                DurationSeconds = request.DurationSeconds,
                Lyrics = request.Lyrics,
                AudioFileUrl = request.AudioFileUrl,
                TrackNumber = request.TrackNumber,
                IsExplicit = request.IsExplicit,
                AlbumId = request.AlbumId,
                ArtistId = request.ArtistId,
                PlayCount = 0,
                LikeCount = 0
            };

            _dbContext.Tracks.Add(track);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Track {TrackId} created successfully", track.Id);
            
            return _mapper.Map<TrackDto>(track);
        }
    }
}
