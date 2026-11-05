using RevalM.SharedStorage.LSData;
using RevalW.Services.DocumentsAPI.Utils;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using System.Diagnostics;
using System.Text;
using SJsonSerializer = RevalM.SharedStorage.Utils.JsonSerializer;

namespace RevalW.Services.DocumentsAPI.Services;

public class DocumentsService(AzurePrivateBlobService privateBlobService, AzurePublicBlobService publicBlobService, ItemsService itemsService, GenerationProgressService progressService)
{
    public string StartCreate(DocumentGenerateRequest? request)
    {
        var operationId = Guid.NewGuid().ToString("N");
        progressService.Create(operationId);

        // run generation in background (fire-and-forget). Exceptions logged into progress
        _ = Task.Run(async () =>
        {
            DocumentCreationResult? result = null;
            try
            {
                progressService.TryAppend(operationId, "Generation started");
                result = await Create(request, operationId);
                progressService.TryAppend(operationId, $"Finished. Result: {(string.IsNullOrEmpty(result?.StdErr) ? result?.OutPath : "err")}");
            }
            catch (Exception ex)
            {
                progressService.TrySetResult(operationId, result ?? new() { StdErr = $"Unhandled error: {ex.Message}" });
            }
        });

        return operationId;
    }
    public async Task<DocumentCreationResult> Create(DocumentGenerateRequest? request, string? operationId = null)
    {
        DocumentCreationResult ret;
        if (request is null)
        {
            ret = DocumentCreationResult.Err("no request data?");
            progressService.TrySetResult(operationId, ret);
            return ret;
        }
        progressService.TryAppend(operationId, "Create started");
        var azureBlobPath = NameGenerator.GetPdfFilesPath(request.OrganizationId);
        var azureBlobDebugPath = NameGenerator.GetPdfDebugPath(request.OrganizationId);
        var workPath = Path.Combine(Path.GetTempPath(), "fop", $"{request.OrganizationId}-{request.DocumentTemplateId}-{request.ItemId}");
        Directory.CreateDirectory(workPath);
        string? dataPath = null;
        string? templatePath = null;
        try
        {
            await ClearDebug(azureBlobDebugPath);
            var data = ReadAppJsonData(request.AppDataJson);
            if (data is null)
            {
                var message = "invalid app data json?";
                await OnErr(azureBlobDebugPath, message, null, null);
                progressService.TryAppend(operationId, message);
                ret = DocumentCreationResult.Err(message);
                progressService.TrySetResult(operationId, ret);
                return ret;
            }

            dataPath = Path.Combine(workPath, dataFileName);
            if (!TryDeleteFile(dataPath))
            {
                var message = "failed to clear storage before document creation";
                await OnErr(azureBlobDebugPath, message, null, null);
                progressService.TryAppend(operationId, message);
                ret = DocumentCreationResult.Err(message);
                progressService.TrySetResult(operationId, ret);
                return ret;
            }

            var creator = new Creator();
            var structuredData = creator.Create(data);
            var printer = new Printer();
            printer.FillAndPrintXML(dataPath, structuredData);
            progressService.TryAppend(operationId, $"XML written to {dataPath}");

            templatePath = Path.Combine(workPath, templateFileName);
            if (!TryDeleteFile(templatePath))
            {
                var message = "failed to clear storage after document creation";
                await OnErr(azureBlobDebugPath, message, null, null);
                progressService.TryAppend(operationId, message);
                ret = DocumentCreationResult.Err(message);
                progressService.TrySetResult(operationId, ret);
                return ret;
            }
            await File.WriteAllTextAsync(templatePath, request.Template);
            progressService.TryAppend(operationId, $"Template written to {templatePath}");

            await DownloadResources(workPath, request.ResourceFiles);
            progressService.TryAppend(operationId, "Resources downloaded");

            var outFileName = NameGenerator.GetPdfFileName(request.DocumentTemplateId, request.ItemId);
            var outPath = Path.Combine(workPath, outFileName);
            progressService.TryAppend(operationId, "Starting FOP to create PDF");

            await CreateDocument(dataPath, templatePath, outPath, operationId);
            progressService.TryAppend(operationId, "FOP finished");

            var blobName = await PrivateUploadLocalFile(azureBlobPath, outFileName, outPath);
            if (string.IsNullOrEmpty(blobName))
            {
                var message = "failed to upload generated document";
                await OnErr(azureBlobDebugPath, message, dataPath, templatePath);
                progressService.TryAppend(operationId, message);
                ret = DocumentCreationResult.Err(message);
                progressService.TrySetResult(operationId, ret);
                return ret;
            }

            await itemsService.NotifyDocumentGenerated(new GeneratedDocumentDto
            {
                ItemId = request.ItemId,
                DocumentId = request.DocumentTemplateId,
                Title = request.DocumentLabel,
                FilePath = blobName,
                FinishAfter = request.TryFinishAfter
            });

            progressService.TryAppend(operationId, $"Upload finished: {blobName}");
            ret = DocumentCreationResult.Ok(blobName);
            progressService.TrySetResult(operationId, ret);
            return ret;
        }
        catch (Exception e)
        {
            await OnErr(azureBlobDebugPath, e.Message, dataPath, templatePath);
            progressService.TryAppend(operationId, $"Exception: {e.Message}");
            ret = DocumentCreationResult.Err(e.Message);
            progressService.TrySetResult(operationId, ret);
            return ret;
        }
        finally
        {
            if (!string.IsNullOrEmpty(workPath))
                Directory.Delete(workPath, true);
            progressService.TryAppend(operationId, "Work directory cleaned");
        }
    }
    private const string dataFileName = "xmlData.xml";
    private const string templateFileName = "template.xsl";

    private string? _fopHome;
    private string FopHome => _fopHome ??= $"{AppContext.BaseDirectory}fop";

    private string? _classPath;
    private string ClassPath => _classPath ??= $"{FopHome}\\build\\*;{FopHome}\\lib\\*;{FopHome}\\target\\*";
    private string GetArgs(string xmlPath, string templatePath, string outPath)
        => $"-cp \"{ClassPath}\" org.apache.fop.cli.Main -xml \"{xmlPath}\" -xsl \"{templatePath}\" -pdf \"{outPath}\" -c \"{FopHome}\\fop.xconf\"";

    private async Task<bool> CreateDocument(string xmlPath, string templatePath, string outPath, string? operationId = null)
    {
        if (!TryDeleteFile(outPath)) return false;

        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(AppContext.BaseDirectory, "jre\\bin\\java"),
            Arguments = GetArgs(xmlPath, templatePath, outPath),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = FopHome
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var sbOut = new StringBuilder();
        var sbErr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e?.Data is null) return;
            sbOut.AppendLine(e.Data);
            progressService.TryAppend(operationId, e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e?.Data is null) return;
            sbErr.AppendLine(e.Data);
            progressService.TryAppend(operationId, "[ERR] " + e.Data);
        };

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var remainingOut = sbOut.ToString();
        if (!string.IsNullOrEmpty(remainingOut))
            progressService.TryAppend(operationId, $"STDOUT (end): {remainingOut}");

        var remainingErr = sbErr.ToString();
        if (!string.IsNullOrEmpty(remainingErr))
            progressService.TryAppend(operationId, $"STDERR (end): {remainingErr}");

        if (!CheckPdfOk(outPath)) return false;
        return true;
    }
    private static bool TryDeleteFile(string fileName)
    {
        if (!File.Exists(fileName)) return true;

        try
        {
            File.Delete(fileName);
        }
        catch
        {
            return false;
        }
        return true;
    }
    public bool CheckPdfOk(string fileName)
    {
        if (!File.Exists(fileName)) return false;
        if (new FileInfo(fileName).Length <= 0) return false;
        return true;
    }
    private static List<ValueWrapper>? ReadAppJsonData(string? appDataJson)
    {
        if (string.IsNullOrEmpty(appDataJson)) return null;
        try
        {
            var _appData = SJsonSerializer.Deserialize<List<ValueWrapper>>(appDataJson) ?? null;
            return _appData;
        }
        catch
        {
            return null;
        }
    }
    private async Task DownloadResources(string workDir, IEnumerable<(string? name, string? blobPath)>? resourceNames)
    {
        if (resourceNames is null) return;

        if (!Directory.Exists(workDir)) Directory.CreateDirectory(workDir);
        foreach ((string? name, string? blobPath) in resourceNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            if (string.IsNullOrEmpty(blobPath)) continue;
            var outPath = Path.Combine(workDir, name);
            var stream = await privateBlobService.DownloadFileAsync(blobPath);
            if (stream is null) continue;
            using var fileStream = File.Create(outPath);
            await stream.CopyToAsync(fileStream);
        }
    }
    private async Task ClearDebug(string workDir)
    {
        await publicBlobService.DeleteFileByBaseNameAsync(workDir);
    }
    private async Task OnErr(string workDir, string genOutput, string? dataPath, string? templatePath)
    {
        await PublicUploadTextFile(workDir, "pdfGenerationOutput.txt", genOutput);
        await PublicUploadLocalFile(workDir, dataFileName, dataPath);
        await PublicUploadLocalFile(workDir, templateFileName, templatePath);
    }
    private Task<string> PrivateUploadTextFile(string workDir, string fileName, string fileContent)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        return privateBlobService.UploadFileAsync(stream, Path.Combine(workDir, fileName).Replace("\\", "/"), true);
    }
    private async Task<string?> PrivateUploadLocalFile(string workDir, string outFileName, string? inPath)
    {
        if (!File.Exists(inPath)) return null;
        await using var stream = File.OpenRead(inPath);
        return await privateBlobService.UploadFileAsync(stream, Path.Combine(workDir, outFileName).Replace("\\", "/"), true);
    }
    private Task<string> PublicUploadTextFile(string workDir, string fileName, string fileContent)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        return publicBlobService.UploadFileAsync(stream, Path.Combine(workDir, fileName).Replace("\\", "/"), true);
    }
    private async Task<string?> PublicUploadLocalFile(string workDir, string outFileName, string? inPath)
    {
        if (!File.Exists(inPath)) return null;
        await using var stream = File.OpenRead(inPath);
        return await publicBlobService.UploadFileAsync(stream, Path.Combine(workDir, outFileName).Replace("\\", "/"), true);
    }
}
