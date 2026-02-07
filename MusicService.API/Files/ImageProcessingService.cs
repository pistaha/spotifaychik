using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace MusicService.API.Files
{
    public sealed class ImageProcessingService
    {
        private static readonly string[] ImageContentTypes =
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp"
        };

        public bool IsImage(string contentType)
        {
            return Array.Exists(ImageContentTypes, x => string.Equals(x, contentType, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<ImageProcessingResult?> ProcessImageAsync(
            string filePath,
            string contentType,
            string thumbnailsDirectory,
            string baseFileNameWithoutExtension,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            using var image = await Image.LoadAsync(filePath, cancellationToken);
            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.XmpProfile = null;

            var width = image.Width;
            var height = image.Height;

            Directory.CreateDirectory(thumbnailsDirectory);

            var smallName = $"{baseFileNameWithoutExtension}_small.jpg";
            var mediumName = $"{baseFileNameWithoutExtension}_medium.jpg";
            var smallPath = Path.Combine(thumbnailsDirectory, smallName);
            var mediumPath = Path.Combine(thumbnailsDirectory, mediumName);

            await SaveResizedAsync(image, smallPath, 200, 200, cancellationToken);
            await SaveResizedAsync(image, mediumPath, 800, 600, cancellationToken);

            await SaveOptimizedAsync(image, filePath, contentType, cancellationToken);

            return new ImageProcessingResult(width, height, smallPath, mediumPath);
        }

        private static Task SaveOptimizedAsync(Image image, string filePath, string contentType, CancellationToken cancellationToken)
        {
            if (string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
            {
                var encoder = new JpegEncoder { Quality = 90 };
                return image.SaveAsync(filePath, encoder, cancellationToken);
            }

            return image.SaveAsync(filePath, cancellationToken);
        }

        private static Task SaveResizedAsync(Image image, string outputPath, int width, int height, CancellationToken cancellationToken)
        {
            using var clone = image.Clone(ctx =>
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Max
                });
            });

            var encoder = new JpegEncoder { Quality = 88 };
            return clone.SaveAsync(outputPath, encoder, cancellationToken);
        }
    }

    public sealed record ImageProcessingResult(int Width, int Height, string SmallPath, string MediumPath);
}
