using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Services;
using Synology.Photos.Slideshow.Api.Tests.Extensions;

namespace Synology.Photos.Slideshow.Api.Tests.UnitTests.ServicesTests;

public class PhotosServiceTests
{
    private sealed record TestHarness(
        PhotosService Service,
        ILocationService LocationService,
        IFileProcessor FileProcessor);

    private static TestHarness CreateHarness(string rootPath, bool enableGeolocation = false)
    {
        var synoApiOptions = new OptionsMonitorStub<SynoApiOptions>(new SynoApiOptions
        {
            DownloadAbsolutePath = rootPath,
            DownloadFileName = "download.zip",
        });

        var thirdPartyOptions = new OptionsMonitorStub<ThirdPartyServices>(new ThirdPartyServices
        {
            EnableGeolocation = enableGeolocation,
        });

        var locationService = Substitute.For<ILocationService>();
        var fileProcessor = Substitute.For<IFileProcessor>();

        var service = new PhotosService(
            synoApiOptions,
            thirdPartyOptions,
            locationService,
            fileProcessor,
            NullLogger<PhotosService>.Instance);

        return new TestHarness(service, locationService, fileProcessor);
    }

    private static async Task WriteWebpAsync(string path, int width = 20, int height = 20, ExifProfile? exif = null)
    {
        using var image = new Image<Rgba32>(width, height);
        if (exif is not null)
            image.Metadata.ExifProfile = exif;
        await image.SaveAsWebpAsync(path, new WebpEncoder { Quality = 75, FileFormat = WebpFileFormatType.Lossy });
    }

    private static async Task WriteJpegAsync(string path, int width = 40, int height = 40, ExifProfile? exif = null)
    {
        using var image = new Image<Rgba32>(width, height);
        if (exif is not null)
            image.Metadata.ExifProfile = exif;
        await image.SaveAsJpegAsync(path, new JpegEncoder { Quality = 75 });
    }

    [Test]
    public async Task Assert_GetThumbnails_Returns_Only_Thumbnail_Files()
    {
        using var temp = new TempDirectory();
        await WriteWebpAsync(Path.Combine(temp.Path, "photo-1.webp"));
        await WriteWebpAsync(Path.Combine(temp.Path, "photo-1__thumb.webp"));
        await WriteWebpAsync(Path.Combine(temp.Path, "photo-2__thumb.webp"));

        var harness = CreateHarness(temp.Path);

        var thumbnails = await harness.Service.GetThumbnails(CancellationToken.None);

        await Assert
            .That(thumbnails.Count)
            .IsEqualTo(2);

        await Assert
            .That(thumbnails.All(t => t.StartsWith("/slideshow/") && t.EndsWith("__thumb.webp")))
            .IsTrue();
    }

    [Test]
    public async Task Assert_GetThumbnails_Returns_Empty_When_No_Thumbnails_Present()
    {
        using var temp = new TempDirectory();
        await WriteWebpAsync(Path.Combine(temp.Path, "photo-1.webp"));

        var harness = CreateHarness(temp.Path);

        var thumbnails = await harness.Service.GetThumbnails(CancellationToken.None);

        await Assert
            .That(thumbnails.Count)
            .IsEqualTo(0);
    }

    [Test]
    public async Task Assert_GetSlides_Excludes_Thumbnails_When_Flag_Is_False()
    {
        using var temp = new TempDirectory();
        await WriteWebpAsync(Path.Combine(temp.Path, "photo.webp"));
        await WriteWebpAsync(Path.Combine(temp.Path, "photo__thumb.webp"));

        var harness = CreateHarness(temp.Path);

        var slides = await harness.Service.GetSlides(includeThumbnails: false, CancellationToken.None);

        await Assert
            .That(slides.Count)
            .IsEqualTo(1);

        await Assert
            .That(slides[0].RelativeUrl)
            .Contains("photo.webp")
            .And
            .DoesNotContain("__thumb");
    }

    [Test]
    public async Task Assert_GetSlides_Includes_Thumbnails_When_Flag_Is_True()
    {
        using var temp = new TempDirectory();
        await WriteWebpAsync(Path.Combine(temp.Path, "photo.webp"));
        await WriteWebpAsync(Path.Combine(temp.Path, "photo__thumb.webp"));

        var harness = CreateHarness(temp.Path);

        var slides = await harness.Service.GetSlides(includeThumbnails: true, CancellationToken.None);

        await Assert
            .That(slides.Count)
            .IsEqualTo(2);
    }

    [Test]
    public async Task Assert_GetSlides_Returns_Empty_Metadata_When_No_Exif()
    {
        using var temp = new TempDirectory();
        await WriteWebpAsync(Path.Combine(temp.Path, "nometa.webp"));

        var harness = CreateHarness(temp.Path);

        var slides = await harness.Service.GetSlides(includeThumbnails: false, CancellationToken.None);

        await Assert
            .That(slides.Count)
            .IsEqualTo(1);

        var slide = slides[0];

        await Assert
            .That(slide.DateTaken)
            .IsEqualTo(string.Empty);

        await Assert
            .That(slide.GoogleMapsLink)
            .IsEqualTo(string.Empty);

        await Assert
            .That(slide.Location)
            .IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Assert_GetSlides_Extracts_Google_Maps_Link_From_Gps_Exif()
    {
        using var temp = new TempDirectory();

        var exif = new ExifProfile();
        exif.SetValue(ExifTag.DateTimeOriginal, "2024:05:01 12:30:45");
        exif.SetValue(ExifTag.OffsetTimeOriginal, "-05:00");
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        exif.SetValue(ExifTag.GPSLatitude, [
            new Rational(36, 1),
            new Rational(9, 1),
            new Rational(36, 1)
        ]);
        exif.SetValue(ExifTag.GPSLongitudeRef, "W");
        exif.SetValue(ExifTag.GPSLongitude, [
            new Rational(86, 1),
            new Rational(46, 1),
            new Rational(48, 1)
        ]);

        await WriteJpegAsync(Path.Combine(temp.Path, "gps.jpg"), exif: exif);

        var harness = CreateHarness(temp.Path, enableGeolocation: false);

        var slides = await harness.Service.GetSlides(includeThumbnails: false, CancellationToken.None);

        await Assert
            .That(slides.Count)
            .IsEqualTo(1);

        var slide = slides[0];

        await Assert
            .That(slide.GoogleMapsLink)
            .StartsWith("https://www.google.com/maps?q=");

        await Assert
            .That(slide.DateTaken)
            .StartsWith("2024-05-01");

        await Assert
            .That(slide.Location)
            .IsEqualTo(string.Empty);

        await harness.LocationService
            .DidNotReceive()
            .GetLocation(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Assert_GetSlides_Calls_LocationService_When_Geolocation_Enabled()
    {
        using var temp = new TempDirectory();

        var exif = new ExifProfile();
        exif.SetValue(ExifTag.DateTimeOriginal, "2024:05:01 12:30:45");
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        exif.SetValue(ExifTag.GPSLatitude, [
            new Rational(36, 1),
            new Rational(0, 1),
            new Rational(0, 1)
        ]);
        exif.SetValue(ExifTag.GPSLongitudeRef, "W");
        exif.SetValue(ExifTag.GPSLongitude, [
            new Rational(86, 1),
            new Rational(0, 1),
            new Rational(0, 1)
        ]);

        await WriteJpegAsync(Path.Combine(temp.Path, "loc.jpg"), exif: exif);

        var harness = CreateHarness(temp.Path, enableGeolocation: true);

        harness.LocationService
            .GetLocation(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns("Nashville, TN");

        var slides = await harness.Service.GetSlides(includeThumbnails: false, CancellationToken.None);

        await Assert
            .That(slides[0].Location)
            .IsEqualTo("Nashville, TN");

        await harness.LocationService
            .Received(1)
            .GetLocation(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Assert_CreateThumbnails_Produces_Thumb_Files()
    {
        using var temp = new TempDirectory();
        await WriteWebpAsync(Path.Combine(temp.Path, "big.webp"), width: 1200, height: 800);

        var harness = CreateHarness(temp.Path);

        await harness.Service.CreateThumbnails(CancellationToken.None);

        var thumbPath = Path.Combine(temp.Path, "big__thumb.webp");

        await Assert
            .That(File.Exists(thumbPath))
            .IsTrue();

        using var thumb = await Image.LoadAsync(thumbPath);

        await Assert
            .That(thumb is { Width: <= 400, Height: <= 400 })
            .IsTrue();
    }

    [Test]
    public async Task Assert_CreateThumbnails_Skips_Files_Already_Thumbnailed()
    {
        using var temp = new TempDirectory();
        await WriteWebpAsync(Path.Combine(temp.Path, "img.webp"), width: 500, height: 500);
        await WriteWebpAsync(Path.Combine(temp.Path, "img__thumb.webp"), width: 100, height: 100);

        var existingThumb = Path.Combine(temp.Path, "img__thumb.webp");
        var originalSize = new FileInfo(existingThumb).Length;

        var harness = CreateHarness(temp.Path);
        await harness.Service.CreateThumbnails(CancellationToken.None);

        var currentSize = new FileInfo(existingThumb).Length;

        await Assert
            .That(currentSize)
            .IsEqualTo(originalSize);
    }

    [Test]
    public async Task Assert_ProcessPhotos_Converts_Sources_To_Webp_And_Requests_Deletion()
    {
        using var temp = new TempDirectory();
        await WriteJpegAsync(Path.Combine(temp.Path, "original.jpg"), width: 200, height: 200);

        var harness = CreateHarness(temp.Path);

        await harness.Service.ProcessPhotos(CancellationToken.None);

        await Assert
            .That(File.Exists(Path.Combine(temp.Path, "original.webp")))
            .IsTrue();

        await harness.FileProcessor
            .Received(1)
            .DeletePhotos(
                Arg.Is<IList<string>>(l => l.Count == 1 && l[0] == "original.jpg"),
                Arg.Any<CancellationToken>());
    }
}
