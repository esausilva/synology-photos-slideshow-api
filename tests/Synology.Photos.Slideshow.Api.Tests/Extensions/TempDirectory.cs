namespace Synology.Photos.Slideshow.Api.Tests.Extensions;

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory(string? prefix = null)
    {
        var folderName = $"{prefix ?? "synology-slideshow-tests"}-{Guid.NewGuid():N}";
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), folderName);
        Directory.CreateDirectory(Path);
    }

    public string WriteFile(string relativePath, string contents)
    {
        var filePath = System.IO.Path.Combine(Path, relativePath);
        var directory = System.IO.Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(filePath, contents);
        return filePath;
    }

    public string WriteFile(string relativePath, byte[] contents)
    {
        var filePath = System.IO.Path.Combine(Path, relativePath);
        var directory = System.IO.Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(filePath, contents);
        return filePath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
