namespace MusicService.API.Diagnostics
{
    public sealed class MemoryStressOptions
    {
        public const string SectionName = "Diagnostics:MemoryStress";

        public bool Enabled { get; set; }

        public int ChunkSizeMb { get; set; } = 16;

        public int MaxChunks { get; set; } = 64;
    }
}
