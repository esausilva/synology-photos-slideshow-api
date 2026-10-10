using Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Request;
using Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Request.Validators;

namespace Synology.Photos.Slideshow.Api.Tests.UnitTests.ValidatorsTests;

public class ToggleFavoriteRequestValidatorTests
{
    private static ToggleFavoriteRequestValidator? _validator;

    [Before(Class)]
    public static void Setup_Validator()
    {
        _validator = new ToggleFavoriteRequestValidator();
    }

    [Test]
    [Arguments("IMG_1.webp")]
    [Arguments("IMG_1__thumb.webp")]
    public async Task Assert_Validator_Passes_For_Plain_Webp_File_Name(string fileName)
    {
        var request = new ToggleFavoriteRequest(fileName, IsFavorite: true, SignalRConnectionId: null);

        var result = await _validator!.ValidateAsync(request);

        await Assert
            .That(result.IsValid)
            .IsTrue();
    }

    [Test]
    [Arguments("")]
    [Arguments("*.webp")]
    [Arguments("IMG_?.webp")]
    [Arguments("../IMG_1.webp")]
    [Arguments("favorites/IMG_1.webp")]
    [Arguments("favorites\\IMG_1.webp")]
    [Arguments("IMG_1.jpg")]
    public async Task Assert_Validator_Fails_For_Invalid_File_Name(string fileName)
    {
        var request = new ToggleFavoriteRequest(fileName, IsFavorite: true, SignalRConnectionId: null);

        var result = await _validator!.ValidateAsync(request);

        await Assert
            .That(result.IsValid)
            .IsFalse();

        await Assert
            .That(result.Errors.Any(e => e.PropertyName == nameof(ToggleFavoriteRequest.FileName)))
            .IsTrue();
    }
}
