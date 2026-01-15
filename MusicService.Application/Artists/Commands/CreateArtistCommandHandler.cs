using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Artists.Dtos;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Artists.Commands
{
    public class CreateArtistCommandHandler : IRequestHandler<CreateArtistCommand, ArtistDto>
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateArtistCommandHandler> _logger;

        public CreateArtistCommandHandler(
            IMusicServiceDbContext dbContext,
            IMapper mapper,
            ILogger<CreateArtistCommandHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ArtistDto> Handle(CreateArtistCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating artist: {Name}", request.Name);

            var artist = new Artist
            {
                Name = request.Name,
                RealName = request.RealName,
                Biography = request.Biography,
                ProfileImage = request.ProfileImage,
                CoverImage = request.CoverImage,
                Genres = request.Genres,
                Country = request.Country,
                CareerStartDate = request.CareerStartDate,
                IsVerified = false,
                MonthlyListeners = 0
            };

            _dbContext.Artists.Add(artist);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Artist {ArtistId} created successfully", artist.Id);
            
            return _mapper.Map<ArtistDto>(artist);
        }
    }
}
