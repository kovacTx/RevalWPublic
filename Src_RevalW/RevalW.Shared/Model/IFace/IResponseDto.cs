namespace RevalW.Shared.Model.IFace;

public interface IResponseDto
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }

}