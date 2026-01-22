using Microsoft.Extensions.Options;
using Synology.Api.Sdk.Constants;
using Synology.Api.Sdk.SynologyApi;
using Synology.Api.Sdk.SynologyApi.FileStation.Request;
using Synology.Api.Sdk.SynologyApi.FileStation.Response;
using Synology.Api.Sdk.SynologyApi.Helpers;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Auth;
using Synology.Photos.Slideshow.Api.Slideshow.Providers;

namespace Synology.Photos.Slideshow.Api.Slideshow.Services;

/// <summary>
/// Represents a service that interacts with the Synology FileStation API to perform file operations
/// such as downloading files. This class provides methods to facilitate downloading images or
/// zipped files from the FileStation API resources.
/// </summary>
public sealed class FileStation : IFileStation
{
    private readonly ISynologyApiInfoProvider _apiInfoProvider;
    private readonly ISynologyApiService _apiService;
    private readonly ISynologyApiRequestBuilder _requestBuilder;
    private readonly ISynologyAuthenticationContext _authContext;
    private readonly IOptionsMonitor<SynoApiOptions> _synoApiOptions;
    private readonly IFileProcessor _fileProcessor;
    private readonly ILogger<FileStation> _logger;

    public FileStation(
        ISynologyApiInfoProvider apiInfoProvider, 
        ISynologyApiService apiService, 
        ISynologyApiRequestBuilder requestBuilder,
        ISynologyAuthenticationContext authContext,
        IOptionsMonitor<SynoApiOptions> synoApiOptions,
        IFileProcessor fileProcessor,
        ILogger<FileStation> logger)
    {
        _apiInfoProvider = apiInfoProvider ?? throw new ArgumentNullException(nameof(apiInfoProvider));
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _requestBuilder = requestBuilder ?? throw new ArgumentNullException(nameof(requestBuilder));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _synoApiOptions = synoApiOptions ?? throw new ArgumentNullException(nameof(synoApiOptions));
        _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
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
        await _fileProcessor.CleanDownloadDirectory(cancellationToken);
        
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
        var apiVersionInfo = await _apiInfoProvider.GetOrFetchInfo(cancellationToken);
        
        return apiVersionInfo.SynoFileStationDownload.MaxVersion;
    }
}