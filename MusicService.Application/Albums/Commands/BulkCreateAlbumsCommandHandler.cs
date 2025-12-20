using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MusicService.Application.Albums.Dtos;
using MusicService.Application.Common.Dtos;
using MusicService.Domain.Entities;
using MusicService.Application.Common.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MusicService.Application.Albums.Commands
{
    public class BulkCreateAlbumsCommandHandler : IRequestHandler<BulkCreateAlbumsCommand, BulkOperationResult<AlbumDto>>
    {
        private readonly IAlbumRepository _albumRepository;
        private readonly IArtistRepository _artistRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<BulkCreateAlbumsCommandHandler> _logger;

        public BulkCreateAlbumsCommandHandler(
            IAlbumRepository albumRepository,
            IArtistRepository artistRepository,
            IMapper mapper,
            ILogger<BulkCreateAlbumsCommandHandler> logger)
        {
            _albumRepository = albumRepository;
            _artistRepository = artistRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BulkOperationResult<AlbumDto>> Handle(BulkCreateAlbumsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bulk creating {Count} albums", request.Commands.Count);

            var result = new BulkOperationResult<AlbumDto>();
            var albumsToCreate = new List<Album>();

            foreach (var command in request.Commands)
            {
                try
                {
                    var artist = await _artistRepository.GetByIdAsync(command.ArtistId, cancellationToken);
                    if (artist == null)
                    {
                        result.Items.Add(new BulkOperationItem<AlbumDto>
                        {
                            Success = false,
                            Message = $"Artist with ID {command.ArtistId} not found",
                            Error = "Artist not found"
                        });
                        continue;
                    }

                    var album = new Album
                    {
                        Title = command.Title,
                        Description = command.Description,
                        CoverImage = command.CoverImage,
                        ReleaseDate = command.ReleaseDate,
                        Type = Enum.Parse<AlbumType>(command.Type),
                        Genres = command.Genres,
                        ArtistId = command.ArtistId
                    };

                    albumsToCreate.Add(album);
                    result.Items.Add(new BulkOperationItem<AlbumDto>
                    {
                        Success = true,
                        Message = "Album queued for creation",
                        Data = _mapper.Map<AlbumDto>(album)
                    });
                }
                catch (Exception ex)
                {
                    result.Items.Add(new BulkOperationItem<AlbumDto>
                    {
                        Success = false,
                        Message = "Error processing album",
                        Error = ex.Message
                    });
                }
            }

            // Создаем все альбомы
            var createdAlbums = new List<AlbumDto>();
            foreach (var album in albumsToCreate)
            {
                try
                {
                    var createdAlbum = await _albumRepository.CreateAsync(album, cancellationToken);
                    createdAlbums.Add(_mapper.Map<AlbumDto>(createdAlbum));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating album in bulk operation");
                }
            }

            result.TotalCount = request.Commands.Count;
            result.SuccessfulCount = createdAlbums.Count;
            result.FailedCount = result.TotalCount - result.SuccessfulCount;

            _logger.LogInformation("Bulk album creation completed: {SuccessfulCount} successful, {FailedCount} failed", 
                result.SuccessfulCount, result.FailedCount);

            return result;
        }
    }
}