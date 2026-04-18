using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Services;
using Synology.Photos.Slideshow.Api.Tests.Extensions;

namespace Synology.Photos.Slideshow.Api.Tests.UnitTests.ServicesTests;

public class FileProcessorTests
{
    private static FileProcessor CreateProcessor(string rootPath, string downloadFileName = "download.zip")
    {
        var options = new OptionsMonitorStub<SynoApiOptions>(new SynoApiOptions
        {
            DownloadAbsolutePath = rootPath,
            DownloadFileName = downloadFileName,
        });

        return new FileProcessor(options, NullLogger<FileProcessor>.Instance);
    }

    [Test]
    public async Task Assert_CleanDownloadDirectory_Deletes_Files_At_Root()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("photo-1.jpg", "binary");
        temp.WriteFile("photo-2.jpg", "binary");

        var processor = CreateProcessor(temp.Path);

        await processor.CleanDownloadDirectory(CancellationToken.None);

        await Assert
            .That(Directory.GetFiles(temp.Path).Length)
            .IsEqualTo(0);
    }

    [Test]
    public async Task Assert_CleanDownloadDirectory_Deletes_Nested_Directories()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("nested", "a.jpg"), "binary");
        temp.WriteFile(Path.Combine("nested", "deeper", "b.jpg"), "binary");

        var processor = CreateProcessor(temp.Path);

        await processor.CleanDownloadDirectory(CancellationToken.None);

        await Assert
            .That(Directory.GetDirectories(temp.Path).Length)
            .IsEqualTo(0);

        await Assert
            .That(Directory.GetFiles(temp.Path, "*", SearchOption.AllDirectories).Length)
            .IsEqualTo(0);
    }

    [Test]
    public async Task Assert_CleanDownloadDirectory_Is_Noop_When_Directory_Missing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}");
        var processor = CreateProcessor(missingPath);

        await Assert
            .That(() => processor.CleanDownloadDirectory(CancellationToken.None))
            .ThrowsNothing();
    }

    [Test]
    public async Task Assert_DeletePhotos_Deletes_Only_Specified_Files()
    {
        using var temp = new TempDirectory();
        var keepPath = temp.WriteFile("keep.webp", "binary");
        temp.WriteFile("delete-1.webp", "binary");
        temp.WriteFile("delete-2.webp", "binary");

        var processor = CreateProcessor(temp.Path);

        await processor.DeletePhotos(["delete-1.webp", "delete-2.webp"], CancellationToken.None);

        await Assert
            .That(File.Exists(keepPath))
            .IsTrue();

        await Assert
            .That(Directory.GetFiles(temp.Path).Length)
            .IsEqualTo(1);
    }

    [Test]
    public async Task Assert_DeletePhotos_Is_Noop_When_Directory_Missing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}");
        var processor = CreateProcessor(missingPath);

        await Assert
            .That(() => processor.DeletePhotos(["not-there.webp"], CancellationToken.None))
            .ThrowsNothing();
    }

    [Test]
    public async Task Assert_ProcessZipFile_Extracts_Flattens_And_Removes_Zip()
    {
        using var temp = new TempDirectory();
        const string zipName = "download.zip";
        var zipPath = Path.Combine(temp.Path, zipName);

        await using (var archive = await ZipFile.OpenAsync(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("outer/inner/nested.txt");
            await using var stream = await entry.OpenAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync("hello");
        }

        var processor = CreateProcessor(temp.Path, downloadFileName: zipName);

        await processor.ProcessZipFile(CancellationToken.None);

        await Assert
            .That(File.Exists(zipPath))
            .IsFalse();

        var flattened = Path.Combine(temp.Path, "nested.txt");
        await Assert
            .That(File.Exists(flattened))
            .IsTrue();

        await Assert
            .That(Directory.GetDirectories(temp.Path).Length)
            .IsEqualTo(0);
    }

    [Test]
    public async Task Assert_ProcessZipFile_Is_Noop_When_Zip_Missing()
    {
        using var temp = new TempDirectory();
        var processor = CreateProcessor(temp.Path);

        await Assert
            .That(() => processor.ProcessZipFile(CancellationToken.None))
            .ThrowsNothing();
    }
}
