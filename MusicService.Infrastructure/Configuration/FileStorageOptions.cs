namespace MusicService.Infrastructure.Configuration
{
    public class FileStorageOptions
    {
        public string DataDirectory { get; set; } = "Data";
        public bool AutoCreateDirectory { get; set; } = true;
        public bool PrettyPrintJson { get; set; } = true;
        public int MaxFileSizeMB { get; set; } = 100;
        public string FileEncoding { get; set; } = "UTF-8";
        public BackupOptions Backup { get; set; } = new BackupOptions();
    }

    public class BackupOptions
    {
        public bool Enabled { get; set; } = true;
        public int MaxBackupCount { get; set; } = 5;
        public string BackupDirectory { get; set; } = "Backups";
    }
}