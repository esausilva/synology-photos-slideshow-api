using Microsoft.Extensions.Options;
using Synology.Api.Sdk.Constants;
using Synology.Api.Sdk.SynologyApi;
using Synology.Api.Sdk.SynologyApi.FileStation.Request;
using Synology.Api.Sdk.SynologyApi.FileStation.Response;
using Synology.Api.Sdk.SynologyApi.Helpers;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Auth;
using Synology.Photos.Slideshow.Api.Slideshow.Common;

namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

/// <summary>
/// Represents a service that interacts with the Synology FileStation API to perform file operations
/// such as downloading files. This class provides methods to facilitate downloading images or
/// zipped files from the FileStation API resources.
/// </summary>
public sealed class FileStation : IFileStation
{
    private readonly ISynologyApiInfo _apiInfo;
    private readonly ISynologyApiService _apiService;
    private readonly ISynologyApiRequestBuilder _requestBuilder;
    private readonly ISynologyAuthenticationContext _authContext;
    private readonly IOptionsMonitor<SynoApiOptions> _synoApiOptions;
    private readonly IFileProcessing _fileProcessing;
    private readonly ILogger<FileStation> _logger;

    public FileStation(
        ISynologyApiInfo apiInfo, 
        ISynologyApiService apiService, 
        ISynologyApiRequestBuilder requestBuilder,
        ISynologyAuthenticationContext authContext,
        IOptionsMonitor<SynoApiOptions> synoApiOptions,
        IFileProcessing fileProcessing,
        ILogger<FileStation> logger)
    {
        _apiInfo = apiInfo ?? throw new ArgumentNullException(nameof(apiInfo));
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _requestBuilder = requestBuilder ?? throw new ArgumentNullException(nameof(requestBuilder));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _synoApiOptions = synoApiOptions ?? throw new ArgumentNullException(nameof(synoApiOptions));
        _fileProcessing = fileProcessing ?? throw new ArgumentNullException(nameof(fileProcessing));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Downloads files from Synology File Station based on the specified file items.
    /// </summary>
    /// <param name="fileStationItems">A list of file station items representing the files to be downloaded.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous download operation.</returns>
    public async Task Download(IList<FileStationItem> fileStationItems, CancellationToken cancellationToken)
    {
        await _fileProcessing.CleanDownloadDirectory(cancellationToken);
        
        var downloadRequest = new FileStationDownloadRequest(
            version: await GetApiVersion(cancellationToken),
            method: SynologyApiMethods.FileStation.Download_Download,
            synoToken: _authContext.GetSynoToken()!,
            mode: "download",
            path: fileStationItems.Select(p => p.Path).ToList());
        var downloadUrl = _requestBuilder.BuildUrl(downloadRequest);
        var downloadResponse = await _apiService.GetRawResponseAsync(downloadUrl, cancellationToken);

        await DownloadHelpers.DownloadImageOrZipFromFileStationApi(
            _synoApiOptions.CurrentValue.DownloadAbsolutePath, 
            fileStationItems.ToList(),
            downloadResponse.HttpResponse);
        
        _logger.LogDebug("Downloaded photos");
    }
    
    /// <summary>
    /// Gets the appropriate API version for file station operations.
    /// </summary>
    private async Task<int> GetApiVersion(CancellationToken cancellationToken)
    {
        var apiVersionInfo = await _apiInfo.GetApiInfo(cancellationToken);
        
        return apiVersionInfo.SynoFileStationDownload.MaxVersion;
    }
}