using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Constants;
using Synology.Photos.Slideshow.Api.Slideshow.Response;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

public sealed partial class PhotosService : IPhotosService
{
    private readonly string _rootPath;
    private readonly ILogger<PhotosService> _logger;

    // Synology creates thumbnails in a directory named "@eaDir" when accessing the photos via 'DS File'
    private const string ThumbnailDir = "@eaDir";

    private static readonly HashSet<string> ValidImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff"
    };

    private static readonly HashSet<string> ImageExtensionsForConversion = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".tiff"
    };

    public PhotosService(IOptionsMonitor<SynoApiOptions> synoApiOptions, ILogger<PhotosService> logger)
    {
        _rootPath = synoApiOptions.CurrentValue.DownloadAbsolutePath;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes photos by resizing and converting them to the WebP format with lossy compression.
    /// Deletes the original photos after processing.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessPhotos(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            _logger.LogInformation("Processing photos");

            var stopwatch = Stopwatch.StartNew();

            var photos = Directory.EnumerateFiles(_rootPath, "*", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}{ThumbnailDir}{Path.DirectorySeparatorChar}") &&
                            !f.Contains($"{Path.DirectorySeparatorChar}{ThumbnailDir}"))
                .Where(f => ImageExtensionsForConversion.Contains(Path.GetExtension(f)));

            foreach (var photo in photos)
            {
                using var image = await Image.LoadAsync(photo, cancellationToken);

                // Keeps original image orientation 
                image.Mutate(i => i.AutoOrient());

                var (scaledWidth, scaledHeight) = ScaleImageDimensions(image, scale: 0.8);
                var filename = Path.GetFileNameWithoutExtension(photo);

                image.Mutate(i => i.Resize(scaledWidth, scaledHeight));

                await image.SaveAsWebpAsync($"{_rootPath}{Path.DirectorySeparatorChar}{filename}.webp", new WebpEncoder
                {
                    Quality = 75,
                    FileFormat = WebpFileFormatType.Lossy
                }, cancellationToken: cancellationToken);

                File.Delete(photo);
            }

            stopwatch.Stop();
            LogPhotosProcessedInElapsedMillisecondsMs(stopwatch.ElapsedMilliseconds);
        }, cancellationToken);
    }

    /// <summary>
    /// Retrieves a list of photo metadata, including relative URLs, capture dates, and Google Maps links.
    /// Filters and processes photos from the configured directory, excluding thumbnails and unsupported formats.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <returns>A task returning a read-only list of <see cref="SlidesResponse"/> objects containing photo metadata.</returns>
    public async Task<IReadOnlyList<SlidesResponse>> GetPhotoRelativeUrls(CancellationToken cancellationToken)
    {
        var slides = await Task.Run(() =>
        {
            _logger.LogInformation("Retrieving photo metadata and creating relative URLs");

            return Directory.EnumerateFiles(_rootPath, "*", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}{ThumbnailDir}{Path.DirectorySeparatorChar}") &&
                            !f.Contains($"{Path.DirectorySeparatorChar}{ThumbnailDir}"))
                .Where(f => ValidImageExtensions.Contains(Path.GetExtension(f)))
                .Select(async f => await CreateImageSlideInfo(f, cancellationToken))
                .Select(t => t.Result)
                .ToList();
        }, cancellationToken);

        return slides;
    }

    private static (int scaledWidth, int scaledHeight) ScaleImageDimensions(Image image, double scale) =>
        ((int)(image.Width * scale), (int)(image.Height * scale));

    private async Task<SlidesResponse> CreateImageSlideInfo(string filePath, CancellationToken cancellationToken)
    {
        var fileName = Path.GetRelativePath(_rootPath, filePath);
        var url = $"{SlideshowConstants.BaseRoute}/{fileName.Replace(Path.DirectorySeparatorChar, '/')}";
        var imageInfo = await Image.IdentifyAsync(filePath, cancellationToken);
        var exifProfile = imageInfo.Metadata.ExifProfile;
        var dateTaken = string.Empty;
        var googleMapsLink = string.Empty;

        if (exifProfile is null)
            return new SlidesResponse(url, dateTaken, googleMapsLink);

        var photoDateTime = GetPhotoDateTimeWithOffset(exifProfile);

        dateTaken = photoDateTime?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? string.Empty;
        googleMapsLink = GetGoogleMapsLink(exifProfile);

        return new SlidesResponse(url, dateTaken, googleMapsLink);
    }

    private static DateTimeOffset? GetPhotoDateTimeWithOffset(ExifProfile exifProfile)
    {
        if (!exifProfile.TryGetValue(ExifTag.DateTimeOriginal, out var dateTimeValue))
            return null;

        if (!DateTime.TryParseExact(
                dateTimeValue.Value,
                "yyyy:MM:dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateTime))
        {
            return null;
        }

        // Try to get timezone offset
        if (exifProfile.TryGetValue(ExifTag.OffsetTimeOriginal, out var offsetValue) &&
            TimeSpan.TryParse(offsetValue.Value, out var offset))
        {
            return new DateTimeOffset(dateTime, offset);
        }

        // No offset available - treat as unspecified (or use TimeSpan.Zero for UTC)
        // When unspecified, the client will translate to local timezone based on the `dateTime`
        return new DateTimeOffset(dateTime);
    }

    private static string GetGoogleMapsLink(ExifProfile profile)
    {
        profile.TryGetValue(ExifTag.GPSLatitude, out var latValues);
        profile.TryGetValue(ExifTag.GPSLatitudeRef, out var latRef);
        profile.TryGetValue(ExifTag.GPSLongitude, out var lonValues);
        profile.TryGetValue(ExifTag.GPSLongitudeRef, out var lonRef);

        if (latValues is null || latRef is null || lonValues is null || lonRef is null)
            return string.Empty;

        // Validate that we have all required parts (Degrees, Minutes, Seconds)
        if (latValues.Value is not [var latD, var latM, var latS] ||
            lonValues.Value is not [var lonD, var lonM, var lonS] ||
            string.IsNullOrEmpty(latRef.Value) || string.IsNullOrEmpty(lonRef.Value))
        {
            return string.Empty;
        }

        var latitude = ToDecimalDegrees(latD, latM, latS, latRef.Value);
        var longitude = ToDecimalDegrees(lonD, lonM, lonS, lonRef.Value);

        return $"https://www.google.com/maps?q={latitude},{longitude}";
    }

    private static double ToDecimalDegrees(Rational degrees, Rational minutes, Rational seconds, string direction)
    {
        var decimalDegrees = degrees.ToDouble() + (minutes.ToDouble() / 60.0) + (seconds.ToDouble() / 3600.0);

        // N/E are positive, S/W are negative
        if (direction is "S" or "W")
        {
            decimalDegrees *= -1;
        }

        return decimalDegrees;
    }

    [LoggerMessage(LogLevel.Information, "Photos processed in {ElapsedMilliseconds}ms")]
    partial void LogPhotosProcessedInElapsedMillisecondsMs(long elapsedMilliseconds);
}