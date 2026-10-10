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
    public async Task Assert_CleanDownloadDirectory_Deletes_Nested_Directories_But_Skips_Favorites()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("nested", "a.jpg"), "binary");
        temp.WriteFile(Path.Combine("favorites", "b.jpg"), "binary");

        var processor = CreateProcessor(temp.Path);

        await processor.CleanDownloadDirectory(CancellationToken.None);

        await Assert
            .That(Directory.Exists(Path.Combine(temp.Path, "nested")))
            .IsFalse();

        await Assert
            .That(Directory.Exists(Path.Combine(temp.Path, "favorites")))
            .IsTrue();

        await Assert
            .That(File.Exists(Path.Combine(temp.Path, "favorites", "b.jpg")))
            .IsTrue();
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
    public async Task Assert_DeletePhotos_Deletes_Specified_Files_And_Their_Thumbnails()
    {
        using var temp = new TempDirectory();
        var keepPath = temp.WriteFile("keep.webp", "binary");
        temp.WriteFile("delete-1.webp", "binary");
        temp.WriteFile("delete-1__thumb.webp", "binary");
        temp.WriteFile("delete-2.webp", "binary");
        temp.WriteFile("delete-2__thumb.webp", "binary");

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
    public async Task Assert_ProcessZipFile_Flattens_And_Skips_Favorites()
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

        temp.WriteFile(Path.Combine("favorites", "saved.txt"), "binary");

        var processor = CreateProcessor(temp.Path, downloadFileName: zipName);

        await processor.ProcessZipFile(CancellationToken.None);

        await Assert
            .That(File.Exists(zipPath))
            .IsFalse();

        var flattened = Path.Combine(temp.Path, "nested.txt");
        await Assert
            .That(File.Exists(flattened))
            .IsTrue();

        var saved = Path.Combine(temp.Path, "favorites", "saved.txt");
        await Assert
            .That(File.Exists(saved))
            .IsTrue();

        await Assert
            .That(Directory.Exists(Path.Combine(temp.Path, "favorites")))
            .IsTrue();
        
        await Assert
            .That(Directory.GetDirectories(temp.Path).Length)
            .IsEqualTo(1);
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

    [Test]
    public async Task Assert_ToggleFavorite_Moves_Photo_And_Thumbnail_Into_Favorites()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("IMG_1.webp", "photo");
        temp.WriteFile("IMG_1__thumb.webp", "thumb");

        var processor = CreateProcessor(temp.Path);

        var result = await processor.ToggleFavorite("IMG_1__thumb.webp", isFavorite: true, CancellationToken.None);

        await Assert.That(result).IsEqualTo("IMG_1.webp");
        await Assert.That(File.Exists(Path.Combine(temp.Path, "favorites", "IMG_1.webp"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(temp.Path, "favorites", "IMG_1__thumb.webp"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(temp.Path, "IMG_1.webp"))).IsFalse();
        await Assert.That(File.Exists(Path.Combine(temp.Path, "IMG_1__thumb.webp"))).IsFalse();
    }

    [Test]
    public async Task Assert_ToggleFavorite_Moves_Photo_And_Thumbnail_Out_Of_Favorites()
    {
        using var temp = new TempDirectory();
        temp.WriteFile(Path.Combine("favorites", "IMG_1.webp"), "photo");
        temp.WriteFile(Path.Combine("favorites", "IMG_1__thumb.webp"), "thumb");

        var processor = CreateProcessor(temp.Path);

        var result = await processor.ToggleFavorite("IMG_1.webp", isFavorite: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo("IMG_1.webp");
        await Assert.That(File.Exists(Path.Combine(temp.Path, "IMG_1.webp"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(temp.Path, "IMG_1__thumb.webp"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(temp.Path, "favorites", "IMG_1.webp"))).IsFalse();
    }

    [Test]
    public async Task Assert_ToggleFavorite_Appends_Suffix_To_Photo_And_Thumbnail_On_Collision()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("IMG_1.webp", "new photo");
        temp.WriteFile("IMG_1__thumb.webp", "new thumb");
        temp.WriteFile(Path.Combine("favorites", "IMG_1.webp"), "existing photo");
        temp.WriteFile(Path.Combine("favorites", "IMG_1__thumb.webp"), "existing thumb");

        var processor = CreateProcessor(temp.Path);

        var result = await processor.ToggleFavorite("IMG_1.webp", isFavorite: true, CancellationToken.None);

        await Assert.That(result).IsEqualTo("IMG_1-2.webp");
        await Assert.That(File.ReadAllText(Path.Combine(temp.Path, "favorites", "IMG_1-2.webp"))).IsEqualTo("new photo");
        await Assert.That(File.ReadAllText(Path.Combine(temp.Path, "favorites", "IMG_1-2__thumb.webp"))).IsEqualTo("new thumb");
        await Assert.That(File.ReadAllText(Path.Combine(temp.Path, "favorites", "IMG_1.webp"))).IsEqualTo("existing photo");
    }

    [Test]
    public async Task Assert_ToggleFavorite_Appends_Suffix_When_Only_Thumbnail_Collides()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("IMG_1.webp", "photo");
        temp.WriteFile(Path.Combine("favorites", "IMG_1__thumb.webp"), "orphan thumb");

        var processor = CreateProcessor(temp.Path);

        var result = await processor.ToggleFavorite("IMG_1.webp", isFavorite: true, CancellationToken.None);

        await Assert.That(result).IsEqualTo("IMG_1-2.webp");
    }

    [Test]
    public async Task Assert_ToggleFavorite_Does_Not_Rename_When_Photo_Already_At_Destination()
    {
        using var temp = new TempDirectory();
        temp.WriteFile("IMG_1.webp", "photo");
        temp.WriteFile("IMG_1__thumb.webp", "thumb");

        var processor = CreateProcessor(temp.Path);

        var result = await processor.ToggleFavorite("IMG_1.webp", isFavorite: false, CancellationToken.None);

        await Assert.That(result).IsEqualTo("IMG_1.webp");
        await Assert.That(File.Exists(Path.Combine(temp.Path, "IMG_1.webp"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(temp.Path, "IMG_1-2.webp"))).IsFalse();
    }

    [Test]
    public async Task Assert_ToggleFavorite_Returns_Null_When_Photo_Not_Found()
    {
        using var temp = new TempDirectory();

        var processor = CreateProcessor(temp.Path);

        var result = await processor.ToggleFavorite("missing.webp", isFavorite: true, CancellationToken.None);

        await Assert.That(result).IsNull();
    }
}
