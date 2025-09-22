namespace Synology.Photos.Slideshow.Api.Slideshow.Common.Errors;

public sealed class SearchTimedOutError(int maxRetryAttempts)
    : ErrorBase($"Search operation did not complete after {maxRetryAttempts} attempts");