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
public sealed partial class FileStation : IFileStation
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
    public async Task<bool> Download(IList<FileStationItem> fileStationItems, CancellationToken cancellationToken)
        => await DownloadCore(fileStationItems, _authContext.GetSynoToken(), cancellationToken);

    /// <summary>
    /// Downloads files from Synology File Station based on the given file station items.
    /// </summary>
    /// <param name="fileStationItems">A list of file station items representing the files to be downloaded.</param>
    /// <param name="synoToken">The authentication token required to access the Synology File Station.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous file download operation.</returns>
    public async Task<bool> Download(IList<FileStationItem> fileStationItems, string synoToken,
        CancellationToken cancellationToken)
        => await DownloadCore(fileStationItems, synoToken, cancellationToken);

    /// <summary>
    /// Executes the core logic for downloading files from Synology File Station, including cleaning the download directory,
    /// sending requests to the API in chunks to avoid URL length limits, and saving the retrieved files locally.
    /// </summary>
    /// <param name="fileStationItems">A list of file station items representing the files to be downloaded.</param>
    /// <param name="synoToken">The authentication token required to access Synology File Station.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during the download operation.</param>
    /// <returns>A task that represents the asynchronous operation of downloading files, returning true if at least one chunk was successful.</returns>
    private async Task<bool> DownloadCore(IList<FileStationItem> fileStationItems, string? synoToken,
        CancellationToken cancellationToken)
    {
        if (fileStationItems.Count == 0)
        {
            _logger.LogWarning("No photos provided for download.");
            return true;
        }

        LogDownloadingPhotoCount(fileStationItems.Count);
        
        await _fileProcessor.CleanDownloadDirectory(cancellationToken);

        const int chunkSize = 50;
        var chunks = fileStationItems.Chunk(chunkSize).ToList();
        var successfulChunksCount = 0;
        var apiVersion = await GetApiVersion(cancellationToken);

        foreach (var chunk in chunks)
        {
            var success = await DownloadAndProcessChunk(chunk, synoToken!, apiVersion, cancellationToken);
            
            if (success)
                successfulChunksCount++;
        }

        var noSuccessInChunks = chunks.Count > 0 && successfulChunksCount == 0;
        if (noSuccessInChunks)
        {
            _logger.LogError("All photo download chunks failed.");
            return false;
        }
        
        LogDownloadChunkResults(successfulChunksCount, chunks.Count);

        return true;
    }

    private async Task<bool> DownloadAndProcessChunk(
        IEnumerable<FileStationItem> chunk, 
        string synoToken, 
        int apiVersion, 
        CancellationToken cancellationToken)
    {
        var chunkList = chunk.ToList();
        var downloadRequest = new FileStationDownloadRequest(
            version: apiVersion,
            method: SynologyApiMethods.FileStation.Download_Download,
            synoToken: synoToken,
            mode: "download",
            path: chunkList.Select(p => p.Path).ToList());
        
        var downloadUrl = _requestBuilder.BuildUrl(downloadRequest);
        var downloadResponse = await _apiService.GetRawResponseAsync(downloadUrl, cancellationToken);

        if (!downloadResponse.Success)
        {
            _logger.LogWarning("Failed to download chunk of {Count} photos. Status code: {StatusCode}", chunkList.Count, downloadResponse.StatusCode);
            return false;
        }

        await DownloadHelpers.DownloadImageOrZipFromFileStationApi(
            _synoApiOptions.CurrentValue.DownloadAbsolutePath, 
            chunkList,
            downloadResponse.HttpResponse);
        
        await _fileProcessor.ProcessZipFile(cancellationToken);
        
        return true;
    }

    /// <summary>
    /// Gets the appropriate API version for file station operations.
    /// </summary>
    private async Task<int> GetApiVersion(CancellationToken cancellationToken)
    {
        var apiVersionInfo = await _apiInfoProvider.GetOrFetchInfo(cancellationToken);
        
        return apiVersionInfo.SynoFileStationDownload.MaxVersion;
    }

    [LoggerMessage(LogLevel.Information, "Downloading {PhotoCount} photos from NAS")]
    partial void LogDownloadingPhotoCount(int photoCount);

    [LoggerMessage(LogLevel.Debug, "Finished downloading and processing all photo chunks. {SuccessfulCount}/{TotalCount} chunks succeeded.")]
    partial void LogDownloadChunkResults(int successfulCount, int totalCount);
}