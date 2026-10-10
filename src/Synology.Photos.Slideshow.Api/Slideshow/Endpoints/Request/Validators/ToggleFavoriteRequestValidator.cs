using FluentValidation;

namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Request.Validators;

public class ToggleFavoriteRequestValidator : AbstractValidator<ToggleFavoriteRequest>
{
    private static readonly char[] DisallowedCharacters = ['*', '?', '/', '\\'];

    public ToggleFavoriteRequestValidator()
    {
        RuleFor(request => request.FileName)
            .NotEmpty()
            .Must(fileName => fileName.IndexOfAny(DisallowedCharacters) < 0 && !fileName.Contains(".."))
            .WithMessage("FileName must be a plain file name without path or wildcard characters")
            .Must(fileName => fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            .WithMessage("FileName must be a .webp file");
    }
}
