using FluentValidation.Results;
using Synology.Photos.Slideshow.Api.Extensions;

namespace Synology.Photos.Slideshow.Api.Tests.UnitTests.ExtensionsTests;

public class ValidationResultExtensionsTests
{
    [Test]
    public async Task Assert_BuildValidationErrors_Returns_Empty_Dictionary_When_No_Errors()
    {
        var result = new ValidationResult();

        var errors = result.BuildValidationErrors();

        await Assert
            .That(errors.Count)
            .IsEqualTo(0);
    }

    [Test]
    public async Task Assert_BuildValidationErrors_Groups_Errors_By_PropertyName()
    {
        var result = new ValidationResult([
            new ValidationFailure("PhotoNames", "must not be null"),
            new ValidationFailure("PhotoNames", "must have at least one item"),
            new ValidationFailure("SignalRConnectionId", "invalid id")
        ]);

        var errors = result.BuildValidationErrors();

        await Assert
            .That(errors)
            .ContainsKey("PhotoNames");

        await Assert
            .That(errors["PhotoNames"].Length)
            .IsEqualTo(2);

        await Assert
            .That(errors["PhotoNames"])
            .Contains("must not be null")
            .And
            .Contains("must have at least one item");

        await Assert
            .That(errors["SignalRConnectionId"].Length)
            .IsEqualTo(1);
    }

    [Test]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments(null)]
    public async Task Assert_BuildValidationErrors_Uses_Request_Key_When_PropertyName_Is_Missing(string? propertyName)
    {
        var result = new ValidationResult([
            new ValidationFailure(propertyName, "bad request")
        ]);

        var errors = result.BuildValidationErrors();

        await Assert
            .That(errors)
            .ContainsKey("request");

        await Assert
            .That(errors["request"])
            .Contains("bad request");
    }
}
