using Microsoft.AspNetCore.Mvc;
using Synology.Photos.Slideshow.Api.Slideshow.Services;

namespace Synology.Photos.Slideshow.Api.Slideshow.Endpoints;

public static class DeletePhoto
{
    public static async Task<IResult> PostAsync([FromBody] IList<string> request,
        IPhotosService photosService, 
        IFileProcessor fileProcessor,
        CancellationToken cancellationToken)
    {
        var photoUrls = await photosService.GetPhotoRelativeUrls(cancellationToken);
        var unmatchedPhotos = 
            (from currentPhoto in request 
                let photoExists = photoUrls.Any(url => url.Contains(currentPhoto, StringComparison.OrdinalIgnoreCase)) 
                where !photoExists 
                select currentPhoto).ToList();
        var photosToDelete = request.Where(p => !unmatchedPhotos.Contains(p)).ToList();
        
        if (photosToDelete.Count == 0)
            return Results.NotFound();

        await fileProcessor.DeletePhotoAsync(photosToDelete, cancellationToken);
        
        return Results.Ok(new { unmatchedPhotos });
    }
}