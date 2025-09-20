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
    private readonly IOptionsMonitor<SynoApiOptions> _optionsMonitor;
    
    private const int SingleItemLimit = 1;
    private const int SearchPollingDelayMs = 200;

    public SynologyApiSearch(
        ISynologyApiInfo apiInfo, 
        ISynologyApiService apiService, 
        ISynologyApiRequestBuilder requestBuilder,
        ISynologyAuthenticationContext authContext,
        IOptionsMonitor<SynoApiOptions> optionsMonitor)
    {
        _apiInfo = apiInfo ?? throw new ArgumentNullException(nameof(apiInfo));
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _requestBuilder = requestBuilder ?? throw new ArgumentNullException(nameof(requestBuilder));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    }
    
    /// <summary>
    /// Searches for and retrieves a collection of random photo paths from the Synology NAS.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of photo paths.</returns>
    /// <exception cref="ArgumentException">Thrown when API parameters are invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the API doesn't return expected data.</exception>
    public async Task<IEnumerable<string>> GetPhotos(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_authContext.GetSynoToken()))
        {
            throw new InvalidOperationException("Authentication token is missing or invalid");
        }
        
        var apiVersion = await GetApiVersion(cancellationToken);
        var searchTaskId = await InitiateSearch(apiVersion, cancellationToken);
        var totalPhotosCount = await GetTotalPhotosCount(searchTaskId, apiVersion, cancellationToken);
        var photoPaths = await GetRandomPhotoPaths(searchTaskId, apiVersion, totalPhotosCount, cancellationToken);
        
        await CleanupSearch(searchTaskId, apiVersion, cancellationToken);
        
        return photoPaths;
    }

    /// <summary>
    /// Gets the appropriate API version for file station operations.
    /// </summary>
    private async Task<int> GetApiVersion(CancellationToken cancellationToken)
    {
        var apiVersionInfo = await _apiInfo.GetApiInfo(cancellationToken);
        var apiVersion = apiVersionInfo.SynoFileStationSearch.MaxVersion;
        
        if (apiVersion <= 0)
        {
            throw new InvalidOperationException("Invalid API version received from server");
        }
        
        return apiVersion;
    }

    /// <summary>
    /// Initiates a search operation on the Synology NAS.
    /// </summary>
    private async Task<string> InitiateSearch(int apiVersion, CancellationToken cancellationToken)
    {
        var startRequest = CreateSearchStartRequest(apiVersion);
        var searchUrl = _requestBuilder.BuildUrl(startRequest);
        var searchStartResponse = await _apiService.GetAsync<FileStationSearchStartResponse>(searchUrl, cancellationToken);
        
        if (string.IsNullOrEmpty(searchStartResponse?.Data?.TaskId))
        {
            throw new InvalidOperationException("Failed to initiate search: no task ID returned");
        }
        
        return searchStartResponse.Data.TaskId;
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
            folderPaths: _optionsMonitor.CurrentValue.FileStationSearchFolders
        );
    }

    /// <summary>
    /// Gets the total count of photos available in the search results.
    /// </summary>
    private async Task<int> GetTotalPhotosCount(string taskId, int apiVersion, CancellationToken cancellationToken)
    {
        var listRequest = CreateSearchListRequest(taskId, apiVersion, offset: 0);
        var searchUrl = _requestBuilder.BuildUrl(listRequest);
        var searchListResponse = await _apiService.GetAsync<FileStationSearchListResponse>(searchUrl, cancellationToken);
        
        const int maxRetryAttempts = 10;
        var retryCount = 0;
        
        while (searchListResponse?.Data?.Finished is false && retryCount < maxRetryAttempts)
        {
            await Task.Delay(SearchPollingDelayMs, cancellationToken);
            searchListResponse = await _apiService.GetAsync<FileStationSearchListResponse>(searchUrl, cancellationToken);
            retryCount++;
        }
        
        if (retryCount >= maxRetryAttempts && searchListResponse?.Data?.Finished is false)
        {
            throw new TimeoutException($"Search operation did not complete after {maxRetryAttempts} attempts");
        }
        
        if (searchListResponse?.Data?.Total is null)
        {
            throw new InvalidOperationException("Failed to get total photo count");
        }
        
        return searchListResponse.Data.Total;
    }

    /// <summary>
    /// Retrieves a collection of random photo paths.
    /// </summary>
    private async Task<IList<string>> GetRandomPhotoPaths(
        string taskId, 
        int apiVersion, 
        int totalPhotosCount, 
        CancellationToken cancellationToken)
    {
        if (totalPhotosCount <= 0)
        {
            throw new InvalidOperationException("No photos found in search results");
        }
        
        var photoPaths = new List<string>();
        var photoDownloadCount = _optionsMonitor.CurrentValue.NumberOfPhotoDownloads;
        
        for (var i = 0; i < photoDownloadCount; i++)
        {
            var photoPath = await GetRandomPhotoPath(taskId, apiVersion, totalPhotosCount, cancellationToken);
            photoPaths.Add(photoPath);
        }
        
        return photoPaths;
    }
    
    /// <summary>
    /// Retrieves a single random photo path.
    /// </summary>
    private async Task<string> GetRandomPhotoPath(
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
        
        if (searchListResponse?.Data?.Files is null || searchListResponse.Data.Files.Count == 0)
        {
            throw new InvalidOperationException("No photos were found in the API response");
        }
        
        return searchListResponse.Data.Files[0].Path;
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
    }
}





