using System;
using System.IO;

namespace Tests.TestUtilities;

internal sealed class TempFileStorage : IDisposable
{
    public TempFileStorage(string? fileName = null)
    {
        Root = Path.Combine(Path.GetTempPath(), "MusicServiceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        FilePath = Path.Combine(Root, fileName ?? "data.json");
    }

    public string Root { get; }
    public string FilePath { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors so tests do not fail because of IO locks
        }
    }
}
