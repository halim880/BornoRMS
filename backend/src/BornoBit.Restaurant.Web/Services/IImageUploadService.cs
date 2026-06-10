using Microsoft.AspNetCore.Components.Forms;

namespace BornoBit.Restaurant.Web.Services;

public interface IImageUploadService
{
    /// <summary>
    /// Validates and saves an uploaded image to wwwroot/uploads and returns the
    /// public relative path (e.g. "/uploads/&lt;guid&gt;.jpg"). Throws on invalid type/size.
    /// </summary>
    Task<string> SaveAsync(IBrowserFile file, CancellationToken cancellationToken = default);
}
