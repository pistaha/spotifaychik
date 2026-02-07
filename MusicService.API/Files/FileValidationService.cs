using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MusicService.API.Files
{
    public sealed class FileValidationService
    {
        private readonly FileStorageOptions _options;

        public FileValidationService(IOptions<FileStorageOptions> options)
        {
            _options = options.Value;
        }
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx",
            ".mp3", ".mp4"
        };

        private static readonly Dictionary<string, string[]> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new[] { "image/jpeg" },
            [".jpeg"] = new[] { "image/jpeg" },
            [".png"] = new[] { "image/png" },
            [".gif"] = new[] { "image/gif" },
            [".webp"] = new[] { "image/webp" },
            [".pdf"] = new[] { "application/pdf" },
            [".doc"] = new[] { "application/msword" },
            [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            [".xls"] = new[] { "application/vnd.ms-excel" },
            [".xlsx"] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            [".mp3"] = new[] { "audio/mpeg" },
            [".mp4"] = new[] { "video/mp4" }
        };

        private static readonly Dictionary<string, List<FileSignature>> SignaturesByExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new List<FileSignature> { new(new byte[] { 0xFF, 0xD8, 0xFF }) },
            [".jpeg"] = new List<FileSignature> { new(new byte[] { 0xFF, 0xD8, 0xFF }) },
            [".png"] = new List<FileSignature> { new(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) },
            [".gif"] = new List<FileSignature> { new(new byte[] { 0x47, 0x49, 0x46, 0x38 }) },
            [".webp"] = new List<FileSignature> { new(new byte[] { 0x52, 0x49, 0x46, 0x46 }, 0), new(new byte[] { 0x57, 0x45, 0x42, 0x50 }, 8) },
            [".pdf"] = new List<FileSignature> { new(new byte[] { 0x25, 0x50, 0x44, 0x46 }) },
            [".doc"] = new List<FileSignature> { new(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }) },
            [".docx"] = new List<FileSignature> { new(new byte[] { 0x50, 0x4B, 0x03, 0x04 }) },
            [".xls"] = new List<FileSignature> { new(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }) },
            [".xlsx"] = new List<FileSignature> { new(new byte[] { 0x50, 0x4B, 0x03, 0x04 }) },
            [".mp3"] = new List<FileSignature>
            {
                new(new byte[] { 0x49, 0x44, 0x33 }),
                new(new byte[] { 0xFF, 0xFB }),
                new(new byte[] { 0xFF, 0xF3 }),
                new(new byte[] { 0xFF, 0xF2 })
            },
            [".mp4"] = new List<FileSignature>
            {
                new(new byte[] { 0x66, 0x74, 0x79, 0x70 }, 4)
            }
        };

        public async Task<FileValidationResult> ValidateAsync(IFormFile file, long maxSizeBytes, CancellationToken cancellationToken)
        {
            if (file == null)
            {
                return FileValidationResult.Fail("file is required");
            }

            if (file.Length <= 0)
            {
                return FileValidationResult.Fail("file is empty");
            }

            if (file.Length > maxSizeBytes)
            {
                return FileValidationResult.Fail("file is too large");
            }

            var fileNameValidation = ValidateFileName(file.FileName);
            if (!fileNameValidation.IsValid)
            {
                return fileNameValidation;
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return FileValidationResult.Fail("file type is not allowed");
            }

            if (_options.AllowAnyFile)
            {
                return FileValidationResult.Ok(extension);
            }

            if (!AllowedExtensions.Contains(extension))
            {
                return FileValidationResult.Fail("file type is not allowed");
            }

            if (ContentTypesByExtension.TryGetValue(extension, out var contentTypes) &&
                contentTypes.Length > 0 &&
                !contentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                return FileValidationResult.Fail("content type does not match");
            }

            var signatureValid = await ValidateSignatureAsync(file, extension, cancellationToken);
            if (!signatureValid)
            {
                return FileValidationResult.Fail("file signature is invalid");
            }

            return FileValidationResult.Ok(extension);
        }

        public FileValidationResult ValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
            {
                return FileValidationResult.Fail("file name is invalid");
            }

            if (fileName.Contains("..", StringComparison.Ordinal) ||
                fileName.Contains('/', StringComparison.Ordinal) ||
                fileName.Contains('\\', StringComparison.Ordinal))
            {
                return FileValidationResult.Fail("file name is invalid");
            }

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return FileValidationResult.Fail("file name is invalid");
            }

            return FileValidationResult.Ok(Path.GetExtension(fileName));
        }

        public async Task<FileValidationResult> ValidateFilePathAsync(string filePath, string fileName, CancellationToken cancellationToken)
        {
            var nameValidation = ValidateFileName(fileName);
            if (!nameValidation.IsValid || nameValidation.Extension == null)
            {
                return FileValidationResult.Fail("file name is invalid");
            }

            var extension = nameValidation.Extension;
            if (_options.AllowAnyFile)
            {
                return FileValidationResult.Ok(extension);
            }

            if (!AllowedExtensions.Contains(extension))
            {
                return FileValidationResult.Fail("file type is not allowed");
            }

            var signatureValid = await ValidateSignatureAsync(filePath, extension, cancellationToken);
            if (!signatureValid)
            {
                return FileValidationResult.Fail("file signature is invalid");
            }

            return FileValidationResult.Ok(extension);
        }

        private static async Task<bool> ValidateSignatureAsync(IFormFile file, string extension, CancellationToken cancellationToken)
        {
            if (!SignaturesByExtension.TryGetValue(extension, out var signatures) || signatures.Count == 0)
            {
                return false;
            }

            var maxBytes = signatures.Max(s => s.Offset + s.Bytes.Length);
            var header = new byte[maxBytes];
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

            if (read < maxBytes)
            {
                return false;
            }

            foreach (var signature in signatures)
            {
                var slice = header.AsSpan(signature.Offset, signature.Bytes.Length);
                if (slice.SequenceEqual(signature.Bytes))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<bool> ValidateSignatureAsync(string filePath, string extension, CancellationToken cancellationToken)
        {
            if (!SignaturesByExtension.TryGetValue(extension, out var signatures) || signatures.Count == 0)
            {
                return false;
            }

            var maxBytes = signatures.Max(s => s.Offset + s.Bytes.Length);
            var header = new byte[maxBytes];

            await using var stream = File.OpenRead(filePath);
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (read < maxBytes)
            {
                return false;
            }

            foreach (var signature in signatures)
            {
                var slice = header.AsSpan(signature.Offset, signature.Bytes.Length);
                if (slice.SequenceEqual(signature.Bytes))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed record FileSignature(byte[] Bytes, int Offset = 0);
    }

    public sealed class FileValidationResult
    {
        private FileValidationResult(bool isValid, string? error, string? extension)
        {
            IsValid = isValid;
            Error = error;
            Extension = extension;
        }

        public bool IsValid { get; }
        public string? Error { get; }
        public string? Extension { get; }

        public static FileValidationResult Ok(string extension) => new(true, null, extension);
        public static FileValidationResult Fail(string error) => new(false, error, null);
    }
}
