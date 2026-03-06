using System.IO.Compression;
using Microsoft.Extensions.Options;
using Synology.Photos.Slideshow.Api.Configuration;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

/// <summary>
/// Provides functionality for file processing operations.
/// </summary>
public sealed partial class FileProcessor : IFileProcessor
{
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
        _logger.LogInformation("Processing zip file");

        var zipPath = Path.Combine(_synoApiOptions.CurrentValue.DownloadAbsolutePath,
            _synoApiOptions.CurrentValue.DownloadFileName);
        var extractPath = _synoApiOptions.CurrentValue.DownloadAbsolutePath;

        if (!File.Exists(zipPath))
        {
            _logger.LogError("File '{ZipPath}' does not exist", zipPath);
            return;
        }

        await UnzipPhotos(zipPath, extractPath, cancellationToken);
        
        if (File.Exists(zipPath))
            File.Delete(zipPath);
        
        await FlattenDirectory(extractPath, cancellationToken);

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

                var photoPath = Path.Combine(rootPath, currentPhoto);
                File.Delete(photoPath);
            }
        }, cancellationToken);
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

                if (fileName.Equals(macOsMetadataFile, StringComparison.OrdinalIgnoreCase))
                    continue;
                
                if (Path.GetDirectoryName(file) == rootPath) 
                    continue;

                var destinationPath = ResolveDestinationPath(rootPath, fileName);

                File.Move(file, destinationPath);
            }

            foreach (var dir in Directory.EnumerateDirectories(rootPath))
            {
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
}