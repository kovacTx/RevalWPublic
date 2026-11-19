using RevalW.Shared.Model.IFace;

namespace RevalW.Shared.Utils;

public static class ResponseDtoExt
{
    public static bool IsOk(this IResponseDto? response)
    {
        if (response is null) return false;
        if (!response.IsSuccess) return false;
        return true;
    }
}
