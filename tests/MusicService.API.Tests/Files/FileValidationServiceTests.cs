using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MusicService.API.Files;
using Xunit;

namespace Tests.MusicService.API.Tests.Files;

public class FileValidationServiceTests
{
    [Fact]
    public async Task ValidateAsync_ShouldAcceptValidJpeg()
    {
        var service = new FileValidationService(Options.Create(new FileStorageOptions { AllowAnyFile = false }));
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 };
        await using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "file", "test.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var result = await service.ValidateAsync(file, 1024 * 1024, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.Extension.Should().Be(".jpg");
    }

    [Fact]
    public async Task ValidateAsync_ShouldRejectBadSignature()
    {
        var service = new FileValidationService(Options.Create(new FileStorageOptions { AllowAnyFile = false }));
        var bytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44 };
        await using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "file", "test.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var result = await service.ValidateAsync(file, 1024 * 1024, CancellationToken.None);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateFilePathAsync_ShouldValidatePdf()
    {
        var service = new FileValidationService(Options.Create(new FileStorageOptions { AllowAnyFile = false }));
        var path = Path.Combine(Path.GetTempPath(), $"{System.Guid.NewGuid()}.pdf");
        await File.WriteAllBytesAsync(path, new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }, CancellationToken.None);

        var result = await service.ValidateFilePathAsync(path, "doc.pdf", CancellationToken.None);

        result.IsValid.Should().BeTrue();
        File.Delete(path);
    }

    [Fact]
    public async Task ValidateAsync_ShouldAcceptMp3WithId3()
    {
        var service = new FileValidationService(Options.Create(new FileStorageOptions { AllowAnyFile = false }));
        var bytes = new byte[] { 0x49, 0x44, 0x33, 0x03, 0x00, 0x00 };
        await using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "file", "track.mp3")
        {
            Headers = new HeaderDictionary(),
            ContentType = "audio/mpeg"
        };

        var result = await service.ValidateAsync(file, 1024 * 1024, CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateFileName_ShouldRejectTraversal()
    {
        var service = new FileValidationService(Options.Create(new FileStorageOptions { AllowAnyFile = false }));

        var result = service.ValidateFileName("../secret.txt");

        result.IsValid.Should().BeFalse();
    }
}
