using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace RevalW.Shared.Utils;

public class MaxFileSizeAttribute(int maxFileSize) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var extValidMsg = TryValidateMaxFileSize(value);
        if (!string.IsNullOrEmpty(extValidMsg)) return new ValidationResult(extValidMsg);
        return ValidationResult.Success;
    }

    private string? TryValidateMaxFileSize(object? value)
    {
        if (value is not IFormFile formFile) return null;
        if (formFile.Length >= maxFileSize * 1024 * 1024)
            return $"Maximum file size is {maxFileSize} MB.";
        return null;
    }
}
