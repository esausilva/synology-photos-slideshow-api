namespace Synology.Photos.Slideshow.Api.Slideshow.Common.Errors;

public abstract class ErrorBase(string message)
{
    public string Message { get; init; } = message;
}
