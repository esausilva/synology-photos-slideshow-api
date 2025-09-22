namespace Synology.Photos.Slideshow.Api.Slideshow.Common.Errors;

public sealed class InvalidApiVersionError(int apiVersion)
    : ErrorBase($"Invalid API version received from server: {apiVersion}");
