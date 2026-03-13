using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Synology.Photos.Slideshow.Api.Extensions;
using Synology.Photos.Slideshow.Api.Slideshow.Endpoints.Request;
using Synology.Photos.Slideshow.Api.Slideshow.Hubs;
using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints;

public static class DeletePhotos
{
    public static async Task<IResult> PostAsync([FromBody] DeletePhotosRequest request,
        IPhotosService photosService, 
        IFileProcessor fileProcessor,
        IHubContext<SlideshowHub, ISlideshowHub> hubContext,
        IValidator<DeletePhotosRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.BuildValidationErrors());
        
        var slides = await photosService.GetSlides(cancellationToken);
        var unmatchedPhotos = 
            (from currentPhoto in request.PhotoNames
                let photoExists = slides.Any(s => s.RelativeUrl.Contains(currentPhoto, StringComparison.OrdinalIgnoreCase)) 
                where !photoExists 
                select currentPhoto).ToList();
        var photosToDelete = request.PhotoNames.Where(p => !unmatchedPhotos.Contains(p)).ToList();
        
        if (photosToDelete.Count == 0)
            return Results.NotFound();

        await fileProcessor.DeletePhotos(photosToDelete, cancellationToken);

        if (request.SignalRConnectionId is not null)
            await hubContext.Clients.AllExcept(request.SignalRConnectionId).RefreshSlideshow();
        else
            await hubContext.Clients.All.RefreshSlideshow();
        
        return Results.Ok(new { unmatchedPhotos });
    }
}