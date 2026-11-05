namespace RevalW.Shared.Model;

public class RshResponseDto
{
    public string Token { get; set; } = null!;
    public string RToken { get; set; } = null!;
    public DateTime? Expiration { get; set; } = null!;
}
