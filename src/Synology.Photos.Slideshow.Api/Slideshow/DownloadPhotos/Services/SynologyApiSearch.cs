using FluentResults;
using Microsoft.Extensions.Options;
using Synology.Api.Sdk.Constants;
using Synology.Api.Sdk.SynologyApi;
using Synology.Api.Sdk.SynologyApi.FileStation.Request;
using Synology.Api.Sdk.SynologyApi.FileStation.Response;
using Synology.Photos.Slideshow.Api.Configuration;
using Synology.Photos.Slideshow.Api.Slideshow.Auth;
using Synology.Photos.Slideshow.Api.Slideshow.Common;

namespace Synology.Photos.Slideshow.Api.Slideshow.DownloadPhotos.Services;

/// <summary>
/// Provides functionality to search and retrieve photos from a Synology NAS.
/// </summary>
public sealed class SynologyApiSearch : ISynologyApiSearch
{
    private readonly ISynologyApiInfo _apiInfo;
    private readonly ISynologyApiService _apiService;
    private readonly ISynologyApiRequestBuilder _requestBuilder;
    private readonly ISynologyAuthenticationContext _authContext;
    private readonly IOptionsMonitor<SynoApiOptions> _synoApiOptions;
    private readonly ILogger<SynologyApiSearch> _logger;

    private static readonly IList<string> VideoFileExtensions = [".mp4", ".mov", ".avi", ".mkv", ".wmv"];

    private const int SingleItemLimit = 1;
    private const int SearchPollingDelayMs = 3_000;

    public SynologyApiSearch(
        ISynologyApiInfo apiInfo, 
        ISynologyApiService apiService, 
        ISynologyApiRequestBuilder requestBuilder,
        ISynologyAuthenticationContext authContext,
        IOptionsMonitor<SynoApiOptions> synoApiOptions, 
        ILogger<SynologyApiSearch> logger)
    {
        _apiInfo = apiInfo ?? throw new ArgumentNullException(nameof(apiInfo));
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _requestBuilder = requestBuilder ?? throw new ArgumentNullException(nameof(requestBuilder));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _synoApiOptions = synoApiOptions ?? throw new ArgumentNullException(nameof(synoApiOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves a collection of photos or an error result if the operation fails.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A Result representing either:
    /// - Success with a collection of FileStationItem or
    /// - Failure with an error message describing the problem.
    /// </returns>
    public async Task<Result<IList<FileStationItem>>> GetPhotos(CancellationToken cancellationToken)
    {
        var searchTimeout = TimeSpan.FromSeconds(_synoApiOptions.CurrentValue.ApiSearchTimeoutInSeconds);
        using var timeoutCts = new CancellationTokenSource(searchTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var combinedToken = linkedCts.Token;

        var taskId = string.Empty;
        var apiVersion = 2;
        
        try
        {
            var apiVersionResult = await GetApiVersion(combinedToken);
            if (apiVersionResult.IsFailed)
                return Result.Fail<IList<FileStationItem>>(apiVersionResult.Errors);
            
            apiVersion = apiVersionResult.Value;

            var searchResult = await InitiateSearch(apiVersion, combinedToken);
            if (searchResult.IsFailed)
                return Result.Fail<IList<FileStationItem>>(searchResult.Errors);

            taskId = searchResult.Value;

            var photoCountResult = await GetTotalPhotosCount(taskId, apiVersion, combinedToken);
            if (photoCountResult.IsFailed)
            {
                await CleanupSearch(taskId, apiVersion, CancellationToken.None);
                return Result.Fail<IList<FileStationItem>>(photoCountResult.Errors);
            }

            var photosCount = photoCountResult.Value;

            var fileStationItems = await GetRandomFileStationItems(taskId, apiVersion, photosCount, combinedToken);

            await CleanupSearch(taskId, apiVersion, CancellationToken.None);

            return Result.Ok(fileStationItems);
        }
        catch (OperationCanceledException)
        {
            if (!string.IsNullOrWhiteSpace(taskId))
                await CleanupSearch(taskId, apiVersion, CancellationToken.None);
            
            throw;
        }
    }

    /// <summary>
    /// Gets the appropriate API version for file station operations.
    /// </summary>
    private async Task<Result<int>> GetApiVersion(CancellationToken cancellationToken)
    {
        var apiVersionInfo = await _apiInfo.GetApiInfo(cancellationToken);
        var apiVersion = apiVersionInfo.SynoFileStationSearch.MaxVersion;

        if (apiVersion > 0)
            return Result.Ok(apiVersion);
        
        _logger.LogWarning("Failed to get API version");
        return Result.Fail<int>($"Failed to get Synology API version. Reported MaxVersion: {apiVersion}");
    }

    /// <summary>
    /// Initiates a search operation on the Synology NAS.
    /// </summary>
    private async Task<Result<string>> InitiateSearch(int apiVersion, CancellationToken cancellationToken)
    {
        var startRequest = CreateSearchStartRequest(apiVersion);
        var searchUrl = _requestBuilder.BuildUrl(startRequest);
        var searchStartResponse = await _apiService.GetAsync<FileStationSearchStartResponse>(searchUrl, cancellationToken);

        if (!string.IsNullOrEmpty(searchStartResponse?.Data?.TaskId))
            return Result.Ok(searchStartResponse.Data.TaskId);

        _logger.LogWarning("Failed to initiate search operation");
        return Result.Fail<string>("Failed to initiate search operation: no task ID returned");
    }

    /// <summary>
    /// Creates a request to start a file search.
    /// </summary>
    private FileStationSearchRequest CreateSearchStartRequest(int apiVersion)
    {
        return new FileStationSearchRequest(
            version: apiVersion,
            method: SynologyApiMethods.FileStation.Search_Start,
            synoToken: _authContext.GetSynoToken()!,
            fileType: "file",
            folderPaths: _synoApiOptions.CurrentValue.FileStationSearchFolders
        );
    }

    /// <summary>
    /// Gets the total count of photos available in the search results.
    /// </summary>
    private async Task<Result<int>> GetTotalPhotosCount(string taskId, int apiVersion, CancellationToken cancellationToken)
    {
        var listRequest = CreateSearchListRequest(taskId, apiVersion, offset: 0);
        var searchUrl = _requestBuilder.BuildUrl(listRequest);
        var searchListResponse = await _apiService.GetAsync<FileStationSearchListResponse>(searchUrl, cancellationToken);
        
        const int maxRetryAttempts = 10;
        var retryCount = 0;
        
        while (searchListResponse?.Data?.Finished is false && retryCount < maxRetryAttempts)
        {
            _logger.LogDebug("Retrying search operation (attempt {RetryCount})", retryCount);

            await Task.Delay(SearchPollingDelayMs, cancellationToken);
            
            searchListResponse = await _apiService.GetAsync<FileStationSearchListResponse>(searchUrl, cancellationToken);
            retryCount++;
        }
        
        if (retryCount >= maxRetryAttempts && searchListResponse?.Data?.Finished is false)
        {
            _logger.LogWarning("Search operation timed out");
            return Result.Fail<int>($"Search operation timed out after {maxRetryAttempts} attempts");
        }

        if (searchListResponse?.Data?.Total is not null and var total)
            return Result.Ok(total.Value);

        _logger.LogWarning("Failed to get total photo count");
        return Result.Ok(0);
    }

    /// <summary>
    /// Retrieves a collection of random photo paths.
    /// </summary>
    private async Task<IList<FileStationItem>> GetRandomFileStationItems(
        string taskId, 
        int apiVersion, 
        int totalPhotosCount, 
        CancellationToken cancellationToken)
    {
        if (totalPhotosCount <= 0)
        {
            _logger.LogWarning("No photos were found in the search results");
            return new List<FileStationItem>();
        }
    
        var fileStationItems = new List<FileStationItem>();
        var photoDownloadCount = _synoApiOptions.CurrentValue.NumberOfPhotoDownloads;
    
        while (fileStationItems.Count < photoDownloadCount)
        {
            var fileStationItem = await GetRandomFileStationItem(taskId, apiVersion, totalPhotosCount, cancellationToken);
        
            if (!string.IsNullOrWhiteSpace(fileStationItem.Path) && 
                VideoFileExtensions.Any(ext => fileStationItem.Path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Skipping video file: {Path}", fileStationItem.Path);
                continue;
            }
        
            if (!string.IsNullOrWhiteSpace(fileStationItem.Path))
                fileStationItems.Add(fileStationItem);
        }
    
        return fileStationItems;
    }
    
    /// <summary>
    /// Retrieves a single random FileStationItem, which includes the path to the photo.
    /// </summary>
    private async Task<FileStationItem> GetRandomFileStationItem(
        string taskId, 
        int apiVersion, 
        int totalPhotosCount, 
        CancellationToken cancellationToken)
    {
        var random = new Random();
        var randomOffset = random.Next(totalPhotosCount);
        
        var listRequest = CreateSearchListRequest(taskId, apiVersion, offset: randomOffset);
        var searchUrl = _requestBuilder.BuildUrl(listRequest);
        var searchListResponse = await _apiService.GetAsync<FileStationSearchListResponse>(searchUrl, cancellationToken);

        if (searchListResponse?.Data?.Files is [var firstFile, ..])
            return firstFile;
        
        _logger.LogWarning("No photos were found in the search results");
        return new FileStationItem();
    }

    /// <summary>
    /// Creates a request to list search results.
    /// </summary>
    private FileStationSearchRequest CreateSearchListRequest(string taskId, int apiVersion, int offset)
    {
        return new FileStationSearchRequest(
            version: apiVersion,
            method: SynologyApiMethods.FileStation.Search_List,
            synoToken: _authContext.GetSynoToken()!,
            limit: SingleItemLimit,
            taskId: taskId,
            offset: offset
        );
    }

    /// <summary>
    /// Cleans up the search task to free up server resources.
    /// </summary>
    private async Task CleanupSearch(string taskId, int apiVersion, CancellationToken cancellationToken)
    {
        var cleanRequest = new FileStationSearchRequest(
            version: apiVersion,
            method: SynologyApiMethods.FileStation.Search_Clean,
            synoToken: _authContext.GetSynoToken()!,
            taskId: taskId
        );
        var searchUrl = _requestBuilder.BuildUrl(cleanRequest);
        
        await _apiService.GetAsync<FileStationSearchCleanResponse>(searchUrl, cancellationToken);
        
        _logger.LogDebug("Cleaned up search task");
    }
}
