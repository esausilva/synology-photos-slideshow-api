using FluentValidation.Results;

namespace Synology.Photos.Slideshow.Api.Extensions;

public static class ValidationResultExtensions
{
    extension(ValidationResult validationResult)
    {
        public IDictionary<string, string[]> BuildValidationErrors()
        {
            return validationResult.Errors
                .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName) ? "request" : error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());
        }
    }
}