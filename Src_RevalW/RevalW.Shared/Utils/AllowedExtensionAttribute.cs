using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Utils;

public class AllowedExtensionAttribute(string[] extensions) : ValidationAttribute
{
    private readonly string[] extensions = extensions;
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var extValidMsg = TryValidateExtension(value);
        if (!string.IsNullOrEmpty(extValidMsg)) return new ValidationResult(extValidMsg);
        return ValidationResult.Success;
    }

    private string? TryValidateExtension(object? value)
    {
        if (value is not IFormFile formFile) return null;
        var extension = Path.GetExtension(formFile.FileName);
        if (extensions.Contains(extension)) return null;
        return "This image extension is not allowed.";
    }
}
