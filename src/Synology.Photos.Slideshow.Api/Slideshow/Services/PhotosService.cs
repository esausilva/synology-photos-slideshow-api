using Microsoft.Extensions.Options;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Constants;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

public sealed class PhotosService : IPhotosService
{
    private readonly IOptionsMonitor<SynoApiOptions> _synoApiOptions;
    
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff"
    };

    public PhotosService(IOptionsMonitor<SynoApiOptions> synoApiOptions)
    {
        _synoApiOptions = synoApiOptions;
    }

    /// <summary>
    /// Get photo relative urls
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<string>> GetPhotoRelativeUrls(CancellationToken cancellationToken)
    {
        // Synology creates thumbnails in a directory named "@eaDir" when looking at the photos via 'DS File'
        const string thumbnailDir = "@eaDir";
        
        var rootPath = _synoApiOptions.CurrentValue.DownloadAbsolutePath;

        var urls = await Task.Run(() =>
        {
            var photoRelativeUrls = Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}{thumbnailDir}{Path.DirectorySeparatorChar}") && 
                            !f.Contains($"{Path.DirectorySeparatorChar}{thumbnailDir}"))
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f)))
                .Select(f =>
                {
                    var relative = Path.GetRelativePath(rootPath, f);
                    var url = $"{SlideshowConstants.BaseRoute}/{relative.Replace(Path.DirectorySeparatorChar, '/')}";
                    return url;
                })
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
            
            return photoRelativeUrls;
        }, cancellationToken);

        return urls;
    }
}