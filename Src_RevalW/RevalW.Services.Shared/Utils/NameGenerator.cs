namespace RevalW.Services.Shared.Utils;

public static class NameGenerator
{
    public static string GetDocumentsResourceFileName(long organizationId, long configId, string fileName)
        => $"{organizationId}/resources/{configId}_{Path.GetFileNameWithoutExtension(fileName)}";
    public static string GetPdfFileName(long templateId, long itemId)
        => $"{templateId}_{itemId}.pdf";
    public static string GetPdfFilesPath(long organizationId)
        => $"{organizationId}/pdfs";
    public static string GetPdfDebugPath(long organizationId)
        => $"{organizationId}/pdfs/debug";
    public static string GetImageName(long organizationId, long configId, string? extension)
        => $"{organizationId}/images/{configId}" + ((string.IsNullOrEmpty(extension)) ? "" : $".{extension}");
    public static string GetDllName(long organizationId, long configId, string platform)
        => $"{organizationId}/dlls/{configId}_{platform}.dll";
}
