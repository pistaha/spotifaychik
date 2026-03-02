using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using MusicService.API.Files;
using MusicService.Application.Common;
using MusicService.Application.Common.Dtos;
using MusicService.Application.Common.Interfaces;
using MusicService.Application.Files.Dtos;
using MusicService.Domain.Entities;

namespace MusicService.API.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly IMusicServiceDbContext _dbContext;
        private readonly FileStorageOptions _options;
        private readonly FileValidationService _validator;
        private readonly ImageProcessingService _imageProcessing;
        private readonly ISecurityAuditService _auditService;
        private readonly IWebHostEnvironment _environment;

        public FilesController(
            IMusicServiceDbContext dbContext,
            IOptions<FileStorageOptions> options,
            FileValidationService validator,
            ImageProcessingService imageProcessing,
            ISecurityAuditService auditService,
            IWebHostEnvironment environment)
        {
            _dbContext = dbContext;
            _options = options.Value;
            _validator = validator;
            _imageProcessing = imageProcessing;
            _auditService = auditService;
            _environment = environment;
        }

        /// <summary>загрузка одного файла</summary>
        /// <param name="file">файл для загрузки</param>
        [HttpPost("upload")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<FileMetadataDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<FileMetadataDto>), 400)]
        public async Task<ActionResult<ApiResponse<FileMetadataDto>>> Upload(
            [FromForm(Name = "file")] IFormFile file,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<FileMetadataDto>.ErrorResult("Invalid user"));
            }

            var validation = await _validator.ValidateAsync(file, _options.MaxFileSizeBytes, cancellationToken);
            if (!validation.IsValid)
            {
                return BadRequest(ApiResponse<FileMetadataDto>.ErrorResult(validation.Error ?? "Invalid file"));
            }

            var saved = await SaveFileAsync(file, userId.Value, false, null, cancellationToken);
            if (saved == null)
            {
                return BadRequest(ApiResponse<FileMetadataDto>.ErrorResult("Failed to upload file"));
            }

            return Ok(ApiResponse<FileMetadataDto>.SuccessResult(MapToDto(saved), "File uploaded"));
        }

        /// <summary>загрузка нескольких файлов</summary>
        /// <param name="files">список файлов</param>
        [HttpPost("upload-multiple")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<List<FileMetadataDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<List<FileMetadataDto>>), 400)]
        public async Task<ActionResult<ApiResponse<List<FileMetadataDto>>>> UploadMultiple(
            [FromForm(Name = "files")] List<IFormFile> files,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<List<FileMetadataDto>>.ErrorResult("Invalid user"));
            }

            if (files == null || files.Count == 0)
            {
                return BadRequest(ApiResponse<List<FileMetadataDto>>.ErrorResult("Files are required"));
            }

            if (files.Count > _options.MaxFilesPerUpload)
            {
                return BadRequest(ApiResponse<List<FileMetadataDto>>.ErrorResult("Too many files"));
            }

            var totalSize = files.Sum(f => f.Length);
            if (totalSize > _options.MaxTotalUploadBytes)
            {
                return BadRequest(ApiResponse<List<FileMetadataDto>>.ErrorResult("Total size exceeded"));
            }

            foreach (var file in files)
            {
                var validation = await _validator.ValidateAsync(file, _options.MaxFileSizeBytes, cancellationToken);
                if (!validation.IsValid)
                {
                    return BadRequest(ApiResponse<List<FileMetadataDto>>.ErrorResult(validation.Error ?? "Invalid file"));
                }
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var savedFiles = new List<FileMetadata>();
            try
            {
                foreach (var file in files)
                {
                    var saved = await SaveFileAsync(file, userId.Value, false, null, cancellationToken);
                    if (saved == null)
                    {
                        throw new InvalidOperationException("Failed to upload file");
                    }

                    savedFiles.Add(saved);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                foreach (var saved in savedFiles)
                {
                    await DeleteFileFromDiskAsync(saved);
                }
                return BadRequest(ApiResponse<List<FileMetadataDto>>.ErrorResult("Upload failed"));
            }

            var result = savedFiles.Select(MapToDto).ToList();
            return Ok(ApiResponse<List<FileMetadataDto>>.SuccessResult(result, "Files uploaded"));
        }

        /// <summary>список файлов пользователя</summary>
        /// <param name="page">номер страницы</param>
        /// <param name="pageSize">размер страницы</param>
        /// <param name="contentType">images или documents или mime</param>
        /// <param name="search">поиск по имени</param>
        /// <param name="sortOrder">asc или desc</param>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<FileMetadataDto>>), 200)]
        public async Task<ActionResult<ApiResponse<PagedResult<FileMetadataDto>>>> GetFiles(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? contentType = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortOrder = "desc",
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<PagedResult<FileMetadataDto>>.ErrorResult("Invalid user"));
            }

            var isAdmin = User.IsInRole("Admin");
            var query = _dbContext.FileMetadatas.AsNoTracking().AsQueryable();
            if (!isAdmin)
            {
                query = query.Where(f => f.UploadedBy == userId.Value);
            }

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                if (contentType.Equals("images", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(f => EF.Functions.ILike(f.ContentType, "image/%"));
                }
                else if (contentType.Equals("documents", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(f => !EF.Functions.ILike(f.ContentType, "image/%"));
                }
                else
                {
                    query = query.Where(f => f.ContentType == contentType);
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(f => EF.Functions.ILike(f.OriginalFileName, $"%{term}%"));
            }

            query = sortOrder?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true
                ? query.OrderBy(f => f.UploadedAt)
                : query.OrderByDescending(f => f.UploadedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip(Math.Max(0, (page - 1) * pageSize))
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var result = items.Select(MapToDto).ToList();
            var paged = PagedResult<FileMetadataDto>.Create(result, totalCount, page, pageSize);
            return Ok(ApiResponse<PagedResult<FileMetadataDto>>.SuccessResult(paged, "Files retrieved"));
        }

        /// <summary>скачивание файла</summary>
        /// <param name="id">идентификатор файла</param>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadFile(Guid id, CancellationToken cancellationToken = default)
        {
            var file = await _dbContext.FileMetadatas.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            if (file == null || IsExpired(file))
            {
                return NotFound();
            }

            var userId = GetUserId();
            if (!CanAccessFile(file, userId))
            {
                return Forbid();
            }

            var absolutePath = GetAbsolutePath(file);
            if (!System.IO.File.Exists(absolutePath))
            {
                return NotFound();
            }

            file.DownloadCount += 1;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await EnqueueFileDownloadAuditAsync(file, cancellationToken);

            var contentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileNameStar = file.OriginalFileName
            };
            Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

            if (file.Size >= _options.StreamingThresholdBytes)
            {
                return await StreamFileInternalAsync(file, absolutePath, cancellationToken);
            }

            return PhysicalFile(absolutePath, file.ContentType, file.OriginalFileName);
        }

        /// <summary>метаданные файла</summary>
        /// <param name="id">идентификатор файла</param>
        [HttpGet("{id:guid}/info")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<FileMetadataDto>), 200)]
        public async Task<ActionResult<ApiResponse<FileMetadataDto>>> GetFileInfo(Guid id, CancellationToken cancellationToken = default)
        {
            var file = await _dbContext.FileMetadatas.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            if (file == null || IsExpired(file))
            {
                return NotFound(ApiResponse<FileMetadataDto>.ErrorResult("File not found"));
            }

            var userId = GetUserId();
            if (!CanAccessFile(file, userId))
            {
                return Forbid();
            }

            return Ok(ApiResponse<FileMetadataDto>.SuccessResult(MapToDto(file), "File metadata retrieved"));
        }

        /// <summary>потоковая отдача файла</summary>
        /// <param name="id">идентификатор файла</param>
        [HttpGet("{id:guid}/stream")]
        [AllowAnonymous]
        [Produces("application/octet-stream")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(typeof(byte[]), 200)]
        public async Task<IActionResult> Stream(Guid id, CancellationToken cancellationToken = default)
        {
            var file = await _dbContext.FileMetadatas.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            if (file == null || IsExpired(file))
            {
                return NotFound();
            }

            var userId = GetUserId();
            if (!CanAccessFile(file, userId))
            {
                return Forbid();
            }

            var absolutePath = GetAbsolutePath(file);
            if (!System.IO.File.Exists(absolutePath))
            {
                return NotFound();
            }

            return await StreamFileInternalAsync(file, absolutePath, cancellationToken);
        }

        /// <summary>превью изображения</summary>
        /// <param name="id">идентификатор файла</param>
        /// <param name="size">small или medium</param>
        [HttpGet("{id:guid}/thumbnail")]
        [AllowAnonymous]
        public async Task<IActionResult> Thumbnail(Guid id, [FromQuery] string size = "small", CancellationToken cancellationToken = default)
        {
            var file = await _dbContext.FileMetadatas.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            if (file == null || IsExpired(file))
            {
                return NotFound();
            }

            var userId = GetUserId();
            if (!CanAccessFile(file, userId))
            {
                return Forbid();
            }

            if (!_imageProcessing.IsImage(file.ContentType))
            {
                return BadRequest("File is not an image");
            }

            var thumbnailPath = size.Equals("medium", StringComparison.OrdinalIgnoreCase)
                ? file.ThumbnailMediumPath
                : file.ThumbnailSmallPath;

            if (string.IsNullOrWhiteSpace(thumbnailPath) || !System.IO.File.Exists(Path.Combine(GetRootPath(), thumbnailPath)))
            {
                var absolutePath = GetAbsolutePath(file);
                var relativeDirectory = file.Path;
                var baseName = Path.GetFileNameWithoutExtension(file.FileName);
                var thumbnailsDirectory = Path.Combine(GetRootPath(), relativeDirectory);
                var processed = await _imageProcessing.ProcessImageAsync(absolutePath, file.ContentType, thumbnailsDirectory, baseName, cancellationToken);
                if (processed == null)
                {
                    return NotFound();
                }

                file.Width = processed.Width;
                file.Height = processed.Height;
                file.ThumbnailSmallPath = Path.Combine(relativeDirectory, Path.GetFileName(processed.SmallPath));
                file.ThumbnailMediumPath = Path.Combine(relativeDirectory, Path.GetFileName(processed.MediumPath));
                await _dbContext.SaveChangesAsync(cancellationToken);

                thumbnailPath = size.Equals("medium", StringComparison.OrdinalIgnoreCase)
                    ? file.ThumbnailMediumPath
                    : file.ThumbnailSmallPath;
            }

            var absoluteThumbPath = Path.Combine(GetRootPath(), thumbnailPath ?? string.Empty);
            if (!System.IO.File.Exists(absoluteThumbPath))
            {
                return NotFound();
            }

            Response.Headers[HeaderNames.CacheControl] = "public,max-age=31536000";
            return PhysicalFile(absoluteThumbPath, "image/jpeg");
        }

        /// <summary>удаление файла</summary>
        /// <param name="id">идентификатор файла</param>
        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            var file = await _dbContext.FileMetadatas.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            if (file == null)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("File not found"));
            }

            var userId = GetUserId();
            if (!CanDeleteFile(file, userId))
            {
                return Forbid();
            }

            await DeleteFileFromDiskAsync(file);
            _dbContext.FileMetadatas.Remove(file);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(ApiResponse<bool>.SuccessResult(true, "File deleted"));
        }

        /// <summary>загрузка большого файла одним чанком</summary>
        /// <param name="file">файл для загрузки</param>
        [HttpPost("upload/chunked")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5L * 1024 * 1024 * 1024)]
        public async Task<ActionResult<ApiResponse<object>>> UploadChunked(
            [FromForm(Name = "file")] IFormFile file,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<object>.ErrorResult("Invalid user"));
            }

            var effectiveFile = file;
            if (effectiveFile == null)
            {
                var form = await Request.ReadFormAsync(cancellationToken);
                effectiveFile = form.Files.GetFile("file") ?? form.Files.GetFile("chunk");
            }

            if (effectiveFile == null || effectiveFile.Length == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("File is required"));
            }

            var uploadId = Guid.NewGuid().ToString("N");
            var totalChunks = 1;
            var chunkIndex = 0;
            var fileName = string.IsNullOrWhiteSpace(effectiveFile.FileName) ? "upload.bin" : effectiveFile.FileName;

            var fileNameValidation = _validator.ValidateFileName(fileName);
            if (!fileNameValidation.IsValid)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid file name"));
            }

            var chunksDirectory = Path.Combine(GetTempPath(), "chunks", uploadId);
            Directory.CreateDirectory(chunksDirectory);
            var chunkPath = Path.Combine(chunksDirectory, $"chunk_{chunkIndex:D6}.part");
            await using (var output = System.IO.File.Create(chunkPath))
            {
                await effectiveFile.CopyToAsync(output, cancellationToken);
            }

            var session = await _dbContext.FileUploadSessions.FirstOrDefaultAsync(s => s.UploadId == uploadId, cancellationToken);
            if (session == null)
            {
                session = new FileUploadSession
                {
                    UploadId = uploadId,
                    UploadedBy = userId.Value,
                    FileName = fileName,
                    TotalChunks = totalChunks,
                    UploadedChunks = 0,
                    TotalSize = 0,
                    IsCompleted = false
                };
                _dbContext.FileUploadSessions.Add(session);
            }

            session.TotalChunks = totalChunks;
            var chunkFiles = Directory.EnumerateFiles(chunksDirectory, "*.part").ToList();
            var totalSize = chunkFiles.Sum(path => new FileInfo(path).Length);
            if (totalSize > _options.MaxTotalUploadBytes)
            {
                System.IO.File.Delete(chunkPath);
                return BadRequest(ApiResponse<object>.ErrorResult("file is too large"));
            }

            session.TotalSize = totalSize;
            session.UploadedChunks = chunkFiles.Count;
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (session.UploadedChunks < totalChunks)
            {
                var progress = new FileUploadProgressDto
                {
                    UploadId = uploadId,
                    UploadedChunks = session.UploadedChunks,
                    TotalChunks = totalChunks,
                    Percent = (int)Math.Round((double)session.UploadedChunks / totalChunks * 100)
                };
                return Ok(ApiResponse<object>.SuccessResult(progress, "Chunk uploaded"));
            }

            var assembledPath = Path.Combine(GetTempPath(), $"{Guid.NewGuid()}_{fileName}");
            await AssembleChunksAsync(chunksDirectory, assembledPath, totalChunks, cancellationToken);

            var assembledInfo = new FileInfo(assembledPath);
            if (assembledInfo.Length > _options.MaxTotalUploadBytes)
            {
                System.IO.File.Delete(assembledPath);
                return BadRequest(ApiResponse<object>.ErrorResult("file is too large"));
            }

            var fileValidation = await _validator.ValidateFilePathAsync(assembledPath, fileName, cancellationToken);
            if (!fileValidation.IsValid)
            {
                System.IO.File.Delete(assembledPath);
                if (Directory.Exists(chunksDirectory))
                {
                    Directory.Delete(chunksDirectory, true);
                }
                return BadRequest(ApiResponse<object>.ErrorResult(fileValidation.Error ?? "Invalid file"));
            }

            var saved = await SaveFileFromPathAsync(assembledPath, fileName, userId.Value, false, null, cancellationToken);
            session.IsCompleted = true;
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (Directory.Exists(chunksDirectory))
            {
                Directory.Delete(chunksDirectory, true);
            }
            System.IO.File.Delete(assembledPath);

            if (saved == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Upload failed"));
            }

            return Ok(ApiResponse<object>.SuccessResult(MapToDto(saved), "Upload completed"));
        }

        /// <summary>загружает файл через поток без multipart.</summary>
        [HttpPost("upload/stream")]
        [Authorize]
        [Consumes("application/octet-stream")]
        [RequestSizeLimit(5L * 1024 * 1024 * 1024)]
        public async Task<ActionResult<ApiResponse<FileMetadataDto>>> UploadStream(
            [FromQuery] string fileName,
            [FromQuery] bool isPublic = false,
            [FromQuery] DateTime? expiresAt = null,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<FileMetadataDto>.ErrorResult("Invalid user"));
            }

            var nameValidation = _validator.ValidateFileName(fileName);
            if (!nameValidation.IsValid)
            {
                return BadRequest(ApiResponse<FileMetadataDto>.ErrorResult(nameValidation.Error ?? "Invalid file name"));
            }

            if (Request.ContentLength.HasValue && Request.ContentLength.Value > _options.MaxFileSizeBytes)
            {
                return BadRequest(ApiResponse<FileMetadataDto>.ErrorResult("file is too large"));
            }

            Directory.CreateDirectory(GetTempPath());
            var tempPath = Path.Combine(GetTempPath(), $"{Guid.NewGuid()}_{fileName}");
            long totalRead = 0;
            var buffer = new byte[81920];

            await using (var output = System.IO.File.Create(tempPath))
            {
                int read;
                while ((read = await Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    totalRead += read;
                    if (totalRead > _options.MaxFileSizeBytes)
                    {
                        output.Close();
                        System.IO.File.Delete(tempPath);
                        return BadRequest(ApiResponse<FileMetadataDto>.ErrorResult("file is too large"));
                    }

                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }

            if (totalRead == 0)
            {
                System.IO.File.Delete(tempPath);
                return BadRequest(ApiResponse<FileMetadataDto>.ErrorResult("file is empty"));
            }

            var fileValidation = await _validator.ValidateFilePathAsync(tempPath, fileName, cancellationToken);
            if (!fileValidation.IsValid)
            {
                System.IO.File.Delete(tempPath);
                return BadRequest(ApiResponse<FileMetadataDto>.ErrorResult(fileValidation.Error ?? "Invalid file"));
            }

            var saved = await SaveFileFromPathAsync(tempPath, fileName, userId.Value, isPublic, expiresAt, cancellationToken);
            if (saved == null)
            {
                return BadRequest(ApiResponse<FileMetadataDto>.ErrorResult("Upload failed"));
            }

            return Ok(ApiResponse<FileMetadataDto>.SuccessResult(MapToDto(saved), "Upload completed"));
        }

        /// <summary>прогресс загрузки</summary>
        /// <param name="uploadId">идентификатор загрузки</param>
        [HttpGet("upload/{uploadId}/progress")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<FileUploadProgressDto>>> UploadProgress(
            string uploadId,
            CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse<FileUploadProgressDto>.ErrorResult("Invalid user"));
            }

            var session = await _dbContext.FileUploadSessions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.UploadId == uploadId && s.UploadedBy == userId.Value, cancellationToken);
            if (session == null)
            {
                return NotFound(ApiResponse<FileUploadProgressDto>.ErrorResult("Upload not found"));
            }

            var progress = new FileUploadProgressDto
            {
                UploadId = session.UploadId,
                UploadedChunks = session.UploadedChunks,
                TotalChunks = session.TotalChunks,
                Percent = session.TotalChunks == 0 ? 0 : (int)Math.Round((double)session.UploadedChunks / session.TotalChunks * 100)
            };

            return Ok(ApiResponse<FileUploadProgressDto>.SuccessResult(progress, "Progress retrieved"));
        }

        private async Task<FileMetadata?> SaveFileAsync(
            IFormFile file,
            Guid userId,
            bool isPublic,
            DateTime? expiresAt,
            CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(file.FileName);
            var tempPath = Path.Combine(GetTempPath(), $"{Guid.NewGuid()}{extension}");
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath) ?? GetTempPath());

            var hash = await SaveToFileAndHashAsync(file, tempPath, cancellationToken);

            var duplicate = await _dbContext.FileMetadatas.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Hash == hash && f.UploadedBy == userId, cancellationToken);
            if (duplicate != null)
            {
                System.IO.File.Delete(tempPath);
                return duplicate;
            }

            var relativeDirectory = GetRelativeDirectory();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var absoluteDirectory = Path.Combine(GetRootPath(), relativeDirectory);
            Directory.CreateDirectory(absoluteDirectory);

            var finalPath = Path.Combine(absoluteDirectory, fileName);
            System.IO.File.Move(tempPath, finalPath);

            return await PersistFileAsync(finalPath, relativeDirectory, fileName, file.FileName, file.ContentType, file.Length, userId, isPublic, expiresAt, hash, cancellationToken);
        }

        private async Task<FileMetadata?> SaveFileFromPathAsync(
            string filePath,
            string originalFileName,
            Guid userId,
            bool isPublic,
            DateTime? expiresAt,
            CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(originalFileName);
            var relativeDirectory = GetRelativeDirectory();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var absoluteDirectory = Path.Combine(GetRootPath(), relativeDirectory);
            Directory.CreateDirectory(absoluteDirectory);
            var finalPath = Path.Combine(absoluteDirectory, fileName);

            var hash = await HashFileAsync(filePath, cancellationToken);
            var duplicate = await _dbContext.FileMetadatas.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Hash == hash && f.UploadedBy == userId, cancellationToken);
            if (duplicate != null)
            {
                System.IO.File.Delete(filePath);
                return duplicate;
            }

            System.IO.File.Move(filePath, finalPath);
            var info = new FileInfo(finalPath);
            var contentType = GetContentTypeFromExtension(extension);

            return await PersistFileAsync(finalPath, relativeDirectory, fileName, originalFileName, contentType, info.Length, userId, isPublic, expiresAt, hash, cancellationToken);
        }

        private async Task<FileMetadata> PersistFileAsync(
            string finalPath,
            string relativeDirectory,
            string fileName,
            string originalFileName,
            string contentType,
            long size,
            Guid userId,
            bool isPublic,
            DateTime? expiresAt,
            string hash,
            CancellationToken cancellationToken)
        {
            int? width = null;
            int? height = null;
            string? smallPath = null;
            string? mediumPath = null;

            if (_imageProcessing.IsImage(contentType))
            {
                var baseName = Path.GetFileNameWithoutExtension(fileName);
                var processed = await _imageProcessing.ProcessImageAsync(finalPath, contentType, Path.Combine(GetRootPath(), relativeDirectory), baseName, cancellationToken);
                if (processed != null)
                {
                    width = processed.Width;
                    height = processed.Height;
                    smallPath = Path.Combine(relativeDirectory, Path.GetFileName(processed.SmallPath));
                    mediumPath = Path.Combine(relativeDirectory, Path.GetFileName(processed.MediumPath));
                }
            }

            var metadata = new FileMetadata
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                OriginalFileName = originalFileName,
                ContentType = contentType,
                Size = size,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow,
                Path = relativeDirectory,
                Hash = hash,
                IsPublic = isPublic,
                ExpiresAt = expiresAt,
                DownloadCount = 0,
                Width = width,
                Height = height,
                ThumbnailSmallPath = smallPath,
                ThumbnailMediumPath = mediumPath
            };

            _dbContext.FileMetadatas.Add(metadata);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return metadata;
        }

        private static async Task<string> SaveToFileAndHashAsync(IFormFile file, string path, CancellationToken cancellationToken)
        {
            var buffer = new byte[81920];
            using var sha = SHA256.Create();
            await using var input = file.OpenReadStream();
            await using var output = System.IO.File.Create(path);
            int read;
            while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                sha.TransformBlock(buffer, 0, read, null, 0);
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            return Convert.ToHexString(sha.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
        }

        private static async Task<string> HashFileAsync(string path, CancellationToken cancellationToken)
        {
            var buffer = new byte[81920];
            using var sha = SHA256.Create();
            await using var input = System.IO.File.OpenRead(path);
            int read;
            while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                sha.TransformBlock(buffer, 0, read, null, 0);
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return Convert.ToHexString(sha.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
        }

        private static async Task AssembleChunksAsync(string chunksDirectory, string outputPath, int totalChunks, CancellationToken cancellationToken)
        {
            await using var output = System.IO.File.Create(outputPath);
            for (var i = 0; i < totalChunks; i++)
            {
                var partPath = Path.Combine(chunksDirectory, $"chunk_{i:D6}.part");
                await using var input = System.IO.File.OpenRead(partPath);
                await input.CopyToAsync(output, cancellationToken);
            }
        }

        private static string GetContentTypeFromExtension(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                _ => "application/octet-stream"
            };
        }

        private async Task<IActionResult> StreamFileInternalAsync(FileMetadata file, string absolutePath, CancellationToken cancellationToken)
        {
            var totalLength = new FileInfo(absolutePath).Length;
            Response.Headers[HeaderNames.AcceptRanges] = "bytes";
            Response.ContentType = file.ContentType;
            Response.Headers[HeaderNames.ContentDisposition] = BuildContentDisposition(file);

            if (!Request.Headers.TryGetValue(HeaderNames.Range, out var rangeHeader))
            {
                Response.Headers[HeaderNames.ContentLength] = totalLength.ToString();
                await using var stream = System.IO.File.OpenRead(absolutePath);
                await WriteToResponseAsync(stream, Response.Body, totalLength, cancellationToken);
                return new EmptyResult();
            }

            if (!TryParseRange(rangeHeader.ToString(), totalLength, out var start, out var end))
            {
                return StatusCode(416);
            }

            var length = end - start + 1;
            Response.StatusCode = 206;
            Response.Headers[HeaderNames.ContentRange] = $"bytes {start}-{end}/{totalLength}";
            Response.Headers[HeaderNames.ContentLength] = length.ToString();

            await using var rangeStream = System.IO.File.OpenRead(absolutePath);
            rangeStream.Seek(start, SeekOrigin.Begin);
            await WriteToResponseAsync(rangeStream, Response.Body, length, cancellationToken);
            return new EmptyResult();
        }

        private static string BuildContentDisposition(FileMetadata file)
        {
            var name = string.IsNullOrWhiteSpace(file.OriginalFileName)
                ? file.FileName
                : file.OriginalFileName;
            name = Path.GetFileName(name);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "download.bin";
            }

            name = name.Replace("\"", string.Empty);
            return $"attachment; filename=\"{name}\"";
        }

        private static async Task WriteToResponseAsync(Stream source, Stream destination, long length, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            var remaining = length;
            while (remaining > 0)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellationToken);
                if (read == 0)
                {
                    break;
                }
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                remaining -= read;
            }

            await destination.FlushAsync(cancellationToken);
        }

        private static bool TryParseRange(string rangeHeader, long totalLength, out long start, out long end)
        {
            start = 0;
            end = totalLength - 1;

            if (!rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var range = rangeHeader["bytes=".Length..].Split('-');
            if (range.Length != 2)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(range[0]) && long.TryParse(range[1], out var suffixLength))
            {
                start = Math.Max(0, totalLength - suffixLength);
                end = totalLength - 1;
            }
            else if (long.TryParse(range[0], out var parsedStart))
            {
                start = parsedStart;
            }

            if (!string.IsNullOrWhiteSpace(range[1]) && long.TryParse(range[1], out var parsedEnd))
            {
                end = parsedEnd;
            }

            if (start < 0 || end >= totalLength || start > end)
            {
                return false;
            }

            return true;
        }

        private async Task EnqueueFileDownloadAuditAsync(FileMetadata file, CancellationToken cancellationToken)
        {
            var details = new { FileId = file.Id, FileName = file.OriginalFileName };
            await _auditService.EnqueueAsync(new SecurityAuditEntry(
                SecurityEventType.FileDownloaded,
                GetUserId(),
                User.FindFirstValue(ClaimTypes.Email),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                true,
                System.Text.Json.JsonSerializer.Serialize(details),
                DateTime.UtcNow), cancellationToken);
        }

        private Task DeleteFileFromDiskAsync(FileMetadata file)
        {
            var absolutePath = GetAbsolutePath(file);
            if (System.IO.File.Exists(absolutePath))
            {
                System.IO.File.Delete(absolutePath);
            }

            if (!string.IsNullOrWhiteSpace(file.ThumbnailSmallPath))
            {
                var small = Path.Combine(GetRootPath(), file.ThumbnailSmallPath);
                if (System.IO.File.Exists(small))
                {
                    System.IO.File.Delete(small);
                }
            }

            if (!string.IsNullOrWhiteSpace(file.ThumbnailMediumPath))
            {
                var medium = Path.Combine(GetRootPath(), file.ThumbnailMediumPath);
                if (System.IO.File.Exists(medium))
                {
                    System.IO.File.Delete(medium);
                }
            }

            return Task.CompletedTask;
        }

        private static bool IsSafeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 100)
            {
                return false;
            }

            foreach (var ch in value)
            {
                if (!char.IsLetterOrDigit(ch) && ch != '-' && ch != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private string GetRootPath()
        {
            return Path.Combine(_environment.ContentRootPath, _options.RootPath);
        }

        private string GetTempPath()
        {
            return Path.Combine(GetRootPath(), "temp");
        }

        private static string GetRelativeDirectory()
        {
            return $"{DateTime.UtcNow:yyyy/MM/dd}";
        }

        private string GetAbsolutePath(FileMetadata file)
        {
            return Path.Combine(GetRootPath(), file.Path, file.FileName);
        }

        private static bool IsExpired(FileMetadata file)
        {
            return file.ExpiresAt.HasValue && file.ExpiresAt.Value <= DateTime.UtcNow;
        }

        private bool CanAccessFile(FileMetadata file, Guid? userId)
        {
            if (file.IsPublic)
            {
                return true;
            }

            if (User.IsInRole("Admin"))
            {
                return true;
            }

            return userId.HasValue && file.UploadedBy == userId.Value;
        }

        private bool CanDeleteFile(FileMetadata file, Guid? userId)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            return userId.HasValue && file.UploadedBy == userId.Value;
        }

        private FileMetadataDto MapToDto(FileMetadata file)
        {
            var url = Url.Action(nameof(DownloadFile), "Files", new { id = file.Id }) ?? $"/api/files/{file.Id}";
            var thumbnail = Url.Action(nameof(Thumbnail), "Files", new { id = file.Id, size = "small" });

            return new FileMetadataDto
            {
                Id = file.Id,
                OriginalFileName = file.OriginalFileName,
                Size = file.Size,
                ContentType = file.ContentType,
                Url = url,
                ThumbnailUrl = _imageProcessing.IsImage(file.ContentType) ? thumbnail : null,
                Width = file.Width,
                Height = file.Height,
                UploadedAt = file.UploadedAt
            };
        }

        private Guid? GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }
}
