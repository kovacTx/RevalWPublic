using RevalW.Services.TemplatesAPI.Model;
using RevalW.Shared.Model;

namespace RevalW.Services.TemplatesAPI.Services.IFace;
public record ImageSaveResult(string? ImageUrl, string? LocalPath);

public interface IImageStorage
{
    /// <summary>
    /// Saves (or sets default) image for the provided config. Returns ImageUrl and LocalPath to store on the entity.
    /// </summary>
    Task<ImageSaveResult> SaveAsync(UpdateImageDto dto, Config config, HttpContext httpContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes any stored image referenced by the config (if applicable).
    /// </summary>
    Task RemoveAsync(Config? config, CancellationToken cancellationToken = default);
}
