using Microsoft.AspNetCore.Components.Forms;

namespace BornoBit.Restaurant.Web.Services;

public class ImageUploadService : IImageUploadService
{
    private const long MaxBytes = 2 * 1024 * 1024; // 2 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    private readonly IWebHostEnvironment _env;

    public ImageUploadService(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Size == 0)
            throw new InvalidOperationException("No file selected.");
        if (file.Size > MaxBytes)
            throw new InvalidOperationException($"Image too large (max {MaxBytes / (1024 * 1024)} MB).");

        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("Unsupported image type. Use JPG, PNG, or WebP.");
        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new InvalidOperationException("Unsupported image type. Use JPG, PNG, or WebP.");

        // Server-generated filename; never trust the client-supplied name.
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var fullPath = Path.Combine(uploadsDir, fileName);

        await using (var input = file.OpenReadStream(MaxBytes, cancellationToken))
        await using (var output = File.Create(fullPath))
        {
            await input.CopyToAsync(output, cancellationToken);
        }

        return $"/uploads/{fileName}";
    }
}
