namespace MusicService.API.Files
{
    public sealed class FileStorageOptions
    {
        public string RootPath { get; set; } = "Storage";
        public long MaxFileSizeBytes { get; set; } = 5_368_709_120;
        public long MaxTotalUploadBytes { get; set; } = 5_368_709_120;
        public int MaxFilesPerUpload { get; set; } = 10;
        public int StreamingThresholdBytes { get; set; } = 10_485_760;
        public bool AllowAnyFile { get; set; }
    }
}
