namespace RevalW.Shared.Model;

public class DocumentCreationResult
{
    public string? StdErr { get; set; }
    public string? OutPath { get; set; }

    public static DocumentCreationResult Ok(string outPath)
        => new()
        {
            OutPath = outPath
        };
    public static DocumentCreationResult Err(string message)
        => new()
        {
            StdErr = message,
        };
    public static DocumentCreationResult Err(string message, string outPath)
        => new()
        {
            StdErr = message,
            OutPath = outPath
        };
}