namespace RevalW.App.Service.IFace;

public interface ITokenProvider
{
    void SetToken(string? token);
    string? GetToken();
    void ClearToken();
}
