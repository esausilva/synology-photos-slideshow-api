using FluentValidation;

namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Request.Validators;

public class DeletePhotosRequestValidator : AbstractValidator<DeletePhotosRequest>
{
    public DeletePhotosRequestValidator()
    {
        RuleFor(request => request.PhotoNames)
            .NotNull();
    }
}