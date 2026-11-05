using RevalW.Services.Shared.Utils;
using RevalW.Services.TemplatesAPI.Model;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;

namespace RevalW.Services.TemplatesAPI.Services;

public class FileStorageService(AzurePrivateBlobService privateBlobService, AzurePublicBlobService publicBlobService)
{

    public async Task UploadImage(UpdateImageDto dto, Config config)
    {
        config.ImageUrl = "";
        if (dto.Image is null)
        {
            UploadDefaultImage(config);
        }
        else
        {
            await TryUploadImageAndAddPaths(dto, config);
        }
        config.LastUpdated = DateTime.UtcNow;
    }
    public async Task TryRemoveImage(Config? config)
    {
        if (config is null) return;
        var baseName = NameGenerator.GetImageName(config.OrganizationId, config.Id, null);
        await publicBlobService.DeleteFileByBaseNameAsync(baseName);
    }
    public async Task TryUploadDllFilesAndAddPaths(UpdateDllDto dto, Config config)
    {
        if (dto.UpdateWDll)
        {
            var fName = await TryUploadFileAndAddPath("w", dto.WDll, config);
            config.WDllPrivatePath = fName;
        }
        if (dto.UpdateADll)
        {
            var fName = await TryUploadFileAndAddPath("a", dto.ADll, config);
            config.ADllPrivatePath = fName;
        }
        if (dto.UpdateIDll)
        {
            var fName = await TryUploadFileAndAddPath("i", dto.IDll, config);
            config.IDllPrivatePath = fName;
        }
    }
    public async Task TryRemoveDllFiles(UpdateDllDto dto, Config? config)
    {
        if (config is null) return;
        if (dto.UpdateWDll) await privateBlobService.DeleteFileByBaseNameAsync(NameGenerator.GetDllName(config.OrganizationId, config.Id, "w"));
        if (dto.UpdateADll) await privateBlobService.DeleteFileByBaseNameAsync(NameGenerator.GetDllName(config.OrganizationId, config.Id, "a"));
        if (dto.UpdateIDll) await privateBlobService.DeleteFileByBaseNameAsync(NameGenerator.GetDllName(config.OrganizationId, config.Id, "i"));
    }
    public async Task<(int status, ResponseDto<(Stream stream, string fileName)> response)> GetDllFileStream(string? path)
    {
        if (string.IsNullOrEmpty(path)) return (StatusCodes.Status404NotFound, ResponseDto<(Stream, string)>.Err("there is no DLL for this Config"));
        var stream = await privateBlobService.DownloadFileAsync(path);
        if (stream is null) return (StatusCodes.Status500InternalServerError, ResponseDto<(Stream, string)>.Err("could not download the DLL"));

        return (StatusCodes.Status200OK, ResponseDto<(Stream, string)>.Ok((stream, path)));
    }
    public async Task<string?> UploadResourceFile(UploadDocumentsResourceFileDto dto)
    {
        if (dto.File is null) return null;
        var fileName = NameGenerator.GetDocumentsResourceFileName(dto.OrganizationId, dto.ConfigId, dto.File.FileName);
        var ext = Path.GetExtension(dto.File.FileName).TrimStart('.');
        return await privateBlobService.UploadFileAsync(dto.File.OpenReadStream(), $"{fileName}.{ext}");
    }
    public Task DeleteResourceFile(string baseName)
        => privateBlobService.DeleteFileByBaseNameAsync(baseName);

    private async Task TryUploadImageAndAddPaths(UpdateImageDto dto, Config config)
    {
        if (dto.Image is null) return;
        var baseName = NameGenerator.GetImageName(config.OrganizationId, config.Id, Path.GetExtension(dto.Image.FileName).TrimStart('.'));
        using var stream = dto.Image.OpenReadStream();
        var publicName = await publicBlobService.UploadFileAsync(stream, baseName);
        config.ImageUrl = publicName;
    }
    private async Task<string?> TryUploadFileAndAddPath(string platform, IFormFile? dll, Config config)
    {
        if (dll is null) return null;
        var baseName = NameGenerator.GetDllName(config.OrganizationId, config.Id, platform);
        using var stream = dll.OpenReadStream();
        return await privateBlobService.UploadFileAsync(stream, baseName);
    }
    private static void UploadDefaultImage(Config config)
    {
        config.ImageUrl = "https://placehold.co/160x80";
    }
}
