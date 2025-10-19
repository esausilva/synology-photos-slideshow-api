using System.IO.Compression;
using Microsoft.Extensions.Options;
using Synology.Photos.Slideshow.Api.Configuration;

namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

public sealed class FileProcessing : IFileProcessing
{
    private readonly IOptionsMonitor<SynoApiOptions> _synoApiOptions;
    private readonly ILogger<FileProcessing> _logger;

    public FileProcessing(IOptionsMonitor<SynoApiOptions> synoApiOptions, ILogger<FileProcessing>  logger)
    {
        _synoApiOptions = synoApiOptions ?? throw new ArgumentNullException(nameof(synoApiOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task CleanDownloadDirectory(CancellationToken cancellationToken)
    {
        var rootPath = _synoApiOptions.CurrentValue.DownloadAbsolutePath;

        if (!Directory.Exists(rootPath))
        {
            _logger.LogWarning("Directory {RootPath} does not exist", rootPath);
            return;
        }

        _logger.LogInformation("Cleaning directory {RootPath}", rootPath);

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
            
            _logger.LogInformation("Directory {RootPath} cleaned", rootPath);
        }, cancellationToken);
    }

    public async Task ProcessZipFile(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing zip file");
        
        var zipPath = Path.Combine(_synoApiOptions.CurrentValue.DownloadAbsolutePath, 
            _synoApiOptions.CurrentValue.DownloadFileName);
        var extractPath = _synoApiOptions.CurrentValue.DownloadAbsolutePath;

        if (!File.Exists(zipPath))
        {
            _logger.LogError("File {ZipPath} does not exist", zipPath);
            return;
        }

        await UnzipPhotos(zipPath, extractPath, cancellationToken);
        await CleanupZipFile(zipPath, cancellationToken);
        await FlattenDirectory(extractPath, cancellationToken);
        
        _logger.LogInformation("Finished processing zip file");
    }

    private static async Task UnzipPhotos(string zipPath, string extractPath, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(zipPath, extractPath, true);
        }, cancellationToken);
    }

    private static async Task FlattenDirectory(string rootPath, CancellationToken cancellationToken)
    {
        const string macOsMetadataFile = ".DS_Store";
        
        await Task.Run(() =>
        {
            var files = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                if (file == Path.GetDirectoryName(file)) continue;

                var fileName = Path.GetFileName(file);
                var destinationPath = Path.Combine(rootPath, fileName);
                
                if (fileName.Equals(macOsMetadataFile, StringComparison.OrdinalIgnoreCase)) 
                    continue;

                if (File.Exists(destinationPath))
                {
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var fileExt = Path.GetExtension(fileName);
                    var counter = 1;

                    while (File.Exists(destinationPath))
                    {
                        fileName = $"{fileNameWithoutExt}_{counter}{fileExt}";
                        destinationPath = Path.Combine(rootPath, fileName);
                        counter++;
                    }
                }

                File.Move(file, destinationPath);
            }

            var directories = Directory.GetDirectories(rootPath);
            foreach (var dir in directories)
            {
                Directory.Delete(dir, true);
            }
        }, cancellationToken);
    }

    private static async Task CleanupZipFile(string zipPath, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
        }, cancellationToken);
    }
}