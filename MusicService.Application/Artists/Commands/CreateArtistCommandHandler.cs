using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Artists.Dtos;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Artists.Commands
{
    public class CreateArtistCommandHandler : IRequestHandler<CreateArtistCommand, ArtistDto>
    {
        private readonly IArtistRepository _artistRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateArtistCommandHandler> _logger;

        public CreateArtistCommandHandler(
            IArtistRepository artistRepository,
            IMapper mapper,
            ILogger<CreateArtistCommandHandler> logger)
        {
            _artistRepository = artistRepository;
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

            var createdArtist = await _artistRepository.CreateAsync(artist, cancellationToken);
            
            _logger.LogInformation("Artist {ArtistId} created successfully", createdArtist.Id);
            
            return _mapper.Map<ArtistDto>(createdArtist);
        }
    }
}