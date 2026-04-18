using Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Request;
using Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Request.Validators;

namespace Synology.Photos.Slideshow.Api.Tests.UnitTests.ValidatorsTests;

public class DeletePhotosRequestValidatorTests
{
    private static DeletePhotosRequestValidator? _validator;

    [Before(Class)]
    public static void Setup_Validator()
    {
        _validator = new DeletePhotosRequestValidator();
    }

    [Test]
    public async Task Assert_Validator_Fails_When_PhotoNames_Is_Null()
    {
        var request = new DeletePhotosRequest(PhotoNames: null!, SignalRConnectionId: null);

        var result = await _validator!.ValidateAsync(request);

        await Assert
            .That(result.IsValid)
            .IsFalse();

        await Assert
            .That(result.Errors.Any(e => e.PropertyName == nameof(DeletePhotosRequest.PhotoNames)))
            .IsTrue();
    }

    [Test]
    public async Task Assert_Validator_Passes_When_PhotoNames_Is_Empty()
    {
        var request = new DeletePhotosRequest(PhotoNames: [], SignalRConnectionId: null);

        var result = await _validator!.ValidateAsync(request);

        await Assert
            .That(result.IsValid)
            .IsTrue();
    }

    [Test]
    public async Task Assert_Validator_Passes_When_PhotoNames_Has_Items()
    {
        var request = new DeletePhotosRequest(
            PhotoNames: ["photo1.webp", "photo2.webp"],
            SignalRConnectionId: "connection-id");

        var result = await _validator!.ValidateAsync(request);

        await Assert
            .That(result.IsValid)
            .IsTrue();
    }

    [Test]
    public async Task Assert_Validator_Passes_When_SignalRConnectionId_Is_Null()
    {
        var request = new DeletePhotosRequest(
            PhotoNames: ["photo1.webp"],
            SignalRConnectionId: null);

        var result = await _validator!.ValidateAsync(request);

        await Assert
            .That(result.IsValid)
            .IsTrue();
    }
}
