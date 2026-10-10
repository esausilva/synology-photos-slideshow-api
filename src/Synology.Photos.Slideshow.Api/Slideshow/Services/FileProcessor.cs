using System.IO.Compression;
using Microsoft.Extensions.Options;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Extensions;
using static Synology.Photos.Slideshow.Api.Constants.SlideshowConstants;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

/// <summary>
/// Provides functionality for file processing operations.
/// </summary>
public sealed partial class FileProcessor : IFileProcessor
{
    // Serializes favorite toggles so concurrent requests cannot race on the same files
    private static readonly SemaphoreSlim ToggleFavoriteLock = new(1, 1);

    private readonly IOptionsMonitor<SynoApiOptions> _synoApiOptions;
    private readonly ILogger<FileProcessor> _logger;

    public FileProcessor(IOptionsMonitor<SynoApiOptions> synoApiOptions, ILogger<FileProcessor> logger)
    {
        _synoApiOptions = synoApiOptions ?? throw new ArgumentNullException(nameof(synoApiOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Cleans the download directory.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task CleanDownloadDirectory(CancellationToken cancellationToken)
    {
        var rootPath = _synoApiOptions.CurrentValue.DownloadAbsolutePath;

        if (!Directory.Exists(rootPath))
        {
            _logger.LogWarning("Directory '{RootPath}' does not exist", rootPath);
            return;
        }

        LogCleaningDirectoryRootPath(rootPath);

        await Task.Run(() =>
        {
            var files = Directory.GetFiles(rootPath);
            foreach (var file in files)
            {
                File.Delete(file);
            }

            var directories = Directory.GetDirectories(rootPath);
            foreach (var dir in directories)
            {
                if (Path.GetFileName(dir).Equals(FavoritesFolderName, StringComparison.OrdinalIgnoreCase))
                    continue;

                Directory.Delete(dir, true);
            }

            LogDirectoryRootPathCleaned(rootPath);
        }, cancellationToken);
    }

    /// <summary>
    /// Unzips the photos zip file and flattens the directory structure.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task ProcessZipFile(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing downloaded zip file");
        
        var rootPath = _synoApiOptions.CurrentValue.DownloadAbsolutePath;
        var zipPath = Path.Combine(rootPath, _synoApiOptions.CurrentValue.DownloadFileName);

        if (!File.Exists(zipPath))
        {
            _logger.LogError("File '{ZipPath}' does not exist", zipPath);
            return;
        }

        await UnzipPhotos(zipPath, rootPath, cancellationToken);
        
        if (File.Exists(zipPath))
            File.Delete(zipPath);
        
        await FlattenDirectory(rootPath, cancellationToken);

        _logger.LogInformation("Finished processing zip file");
    }

    /// <summary>
    /// Deletes the specified photos from the download directory.
    /// </summary>
    /// <param name="photosToDelete">The list of photo names to delete from the download directory.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeletePhotos(IList<string> photosToDelete, CancellationToken cancellationToken)
    {
        var rootPath = _synoApiOptions.CurrentValue.DownloadAbsolutePath;

        if (!Directory.Exists(rootPath))
        {
            _logger.LogWarning("Directory '{RootPath}' does not exist", rootPath);
            return;
        }

        await Task.Run(() =>
        {
            foreach (var currentPhoto in photosToDelete)
            {
                LogDeletingPhotoWithNameName(currentPhoto);

                var photoPath = FindFilePath(rootPath, currentPhoto);

                if (photoPath is null) 
                    continue;
                
                File.Delete(photoPath);

                if (currentPhoto.EndsWith($"{ThumbnailPostfix}.webp", StringComparison.OrdinalIgnoreCase)) 
                    continue;
                    
                var thumbnailName = $"{Path.GetFileNameWithoutExtension(currentPhoto)}{ThumbnailPostfix}.webp";
                var thumbnailPath = FindFilePath(rootPath, thumbnailName);

                if (thumbnailPath is null) 
                    continue;
                    
                LogDeletingPhotoWithNameName(thumbnailName);
                File.Delete(thumbnailPath);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Toggles the favorite status of a photo by moving the photo and its corresponding thumbnail
    /// in or out of the Favorites folder. When the destination already contains a photo or thumbnail
    /// with the same name, a "-2", "-3", ... suffix is appended to both files.
    /// </summary>
    /// <param name="photoName">The name of the photo to toggle. This can be the photo name or the thumbnail name.</param>
    /// <param name="isFavorite">True to move the photo into the favorites folder; false to move it to the root photos' folder.</param>
    /// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
    /// <returns>The photo file name at its destination, or null when the photo was not found.</returns>
    public async Task<string?> ToggleFavorite(string photoName, bool isFavorite, CancellationToken cancellationToken)
    {
        var rootPath = _synoApiOptions.CurrentValue.DownloadAbsolutePath;
        var favoritesPath = Path.Combine(rootPath, FavoritesFolderName);
        var sourceDir = isFavorite ? rootPath : favoritesPath;
        var destDir = isFavorite ? favoritesPath : rootPath;

        var photoFileName = ToPhotoFileName(photoName);
        var thumbnailFileName = ToThumbnailFileName(photoFileName);

        await ToggleFavoriteLock.WaitAsync(cancellationToken);

        try
        {
            return await Task.Run(() =>
            {
                var sourcePhotoPath = Path.Combine(sourceDir, photoFileName);
                var sourceThumbnailPath = Path.Combine(sourceDir, thumbnailFileName);

                if (!File.Exists(sourcePhotoPath))
                {
                    // Already at the destination (e.g. a repeated request), nothing to move
                    return File.Exists(Path.Combine(destDir, photoFileName)) ? photoFileName : null;
                }

                Directory.CreateDirectory(destDir);

                var (destPhotoPath, destThumbnailPath) = GetUniqueDestinationPaths(destDir, photoFileName);

                LogMovingPhotoFromSourceToDestination(sourcePhotoPath, destPhotoPath);
                File.Move(sourcePhotoPath, destPhotoPath);

                if (File.Exists(sourceThumbnailPath))
                    File.Move(sourceThumbnailPath, destThumbnailPath);

                return Path.GetFileName(destPhotoPath);
            }, cancellationToken);
        }
        finally
        {
            ToggleFavoriteLock.Release();
        }
    }

    private static string ToPhotoFileName(string fileName)
    {
        const string thumbnailSuffix = $"{ThumbnailPostfix}.webp";

        return fileName.EndsWith(thumbnailSuffix, StringComparison.OrdinalIgnoreCase)
            ? $"{fileName[..^thumbnailSuffix.Length]}.webp"
            : fileName;
    }

    private static string ToThumbnailFileName(string photoFileName) =>
        $"{Path.GetFileNameWithoutExtension(photoFileName)}{ThumbnailPostfix}.webp";

    /// <summary>
    /// Resolves destination paths for a photo and its thumbnail so that neither collides with an existing file.
    /// E.g. IMG_1.webp / IMG_1__thumb.webp become IMG_1-2.webp / IMG_1-2__thumb.webp.
    /// </summary>
    private static (string PhotoPath, string ThumbnailPath) GetUniqueDestinationPaths(string destDir, string photoFileName)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(photoFileName);
        var extension = Path.GetExtension(photoFileName);
        var candidateName = photoFileName;
        var counter = 2;

        while (File.Exists(Path.Combine(destDir, candidateName)) ||
               File.Exists(Path.Combine(destDir, ToThumbnailFileName(candidateName))))
        {
            candidateName = $"{nameWithoutExt}-{counter}{extension}";
            counter++;
        }

        return (Path.Combine(destDir, candidateName), Path.Combine(destDir, ToThumbnailFileName(candidateName)));
    }

    private static string? FindFilePath(string rootPath, string fileName)
    {
        return Directory
            .EnumerateFiles(rootPath, fileName, SearchOption.AllDirectories)
            .FirstOrDefault(StringExtensions.IsNotDsmThumbnailDir);
    }

    private static async Task UnzipPhotos(string zipPath, string extractPath, CancellationToken cancellationToken)
    {
        await ZipFile.ExtractToDirectoryAsync(zipPath, extractPath, true, cancellationToken);
    }

    private static async Task FlattenDirectory(string rootPath, CancellationToken cancellationToken)
    {
        const string macOsMetadataFile = ".DS_Store";

        await Task.Run(() =>
        {
            foreach (var file in Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                var relativePath = Path.GetRelativePath(rootPath, file);

                if (fileName.Equals(macOsMetadataFile, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (relativePath.StartsWith($"{FavoritesFolderName}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    continue;
                
                if (Path.GetDirectoryName(file) == rootPath) 
                    continue;

                var destinationPath = ResolveDestinationPath(rootPath, fileName);

                File.Move(file, destinationPath);
            }

            foreach (var dir in Directory.EnumerateDirectories(rootPath))
            {
                if (Path.GetFileName(dir).Equals(FavoritesFolderName, StringComparison.OrdinalIgnoreCase))
                    continue;

                Directory.Delete(dir, true);
            }
        }, cancellationToken);
    }

    private static string ResolveDestinationPath(string rootPath, string fileName)
    {
        var destination = Path.Combine(rootPath, fileName);

        if (!File.Exists(destination))
            return destination;
        
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var counter = 1;

        do
        {
            destination = Path.Combine(rootPath, $"{nameWithoutExt}_{counter}{ext}");
            counter++;
        } while (File.Exists(destination));

        return destination;
    }

    [LoggerMessage(LogLevel.Information, "Cleaning directory '{RootPath}'")]
    partial void LogCleaningDirectoryRootPath(string rootPath);

    [LoggerMessage(LogLevel.Information, "Directory '{RootPath}' cleaned")]
    partial void LogDirectoryRootPathCleaned(string rootPath);

    [LoggerMessage(LogLevel.Information, "Deleting photo with name '{name}'")]
    partial void LogDeletingPhotoWithNameName(string name);

    [LoggerMessage(LogLevel.Information, "Moving photo from '{Source}' to '{Destination}'")]
    partial void LogMovingPhotoFromSourceToDestination(string source, string destination);
}