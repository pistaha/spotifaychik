namespace MusicService.Application.Files.Dtos
{
    public sealed class FileUploadProgressDto
    {
        /// <summary>upload id</summary>
        public string UploadId { get; set; } = string.Empty;
        /// <summary>загружено частей</summary>
        public int UploadedChunks { get; set; }
        /// <summary>всего частей</summary>
        public int TotalChunks { get; set; }
        /// <summary>процент загрузки</summary>
        public int Percent { get; set; }
    }
}
