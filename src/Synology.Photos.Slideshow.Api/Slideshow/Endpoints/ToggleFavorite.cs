using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Synology.Photos.Slideshow.Api.Extensions;
using Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Request;
using Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Response;
using Synology.Photos.Slideshow.Api.Slideshow.Messaging;
using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints;

public static class ToggleFavorite
{
    public static async Task<IResult> PostAsync([FromBody] ToggleFavoriteRequest request,
        IFileProcessor fileProcessor,
        IFavoritesRefreshSignalChannel signalChannel,
        IValidator<ToggleFavoriteRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.BuildValidationErrors());

        var newFileName = await fileProcessor.ToggleFavorite(request.FileName, request.IsFavorite, cancellationToken);

        if (newFileName is null)
            return Results.NotFound();

        await signalChannel.PublishAsync(request.SignalRConnectionId, cancellationToken);

        return Results.Ok(new ToggleFavoriteResponse(newFileName));
    }
}
