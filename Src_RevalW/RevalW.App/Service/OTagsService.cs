using RevalW.App.Service.IFace;
using RevalW.App.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;

namespace RevalW.App.Service;

public class OTagsService(IBaseService baseService, CommonData cd)
{
    public async Task<ResponseDto<IEnumerable<PublicOrganizationTagDto>?>?> GetAllFull()
    {
        return await baseService.SendAsync<IEnumerable<PublicOrganizationTagDto>>(new RequestDto(cd.OTagsAPIUrl + $"/allFull")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<IEnumerable<TagNameDto>?>?> GetAll()
    {
        return await baseService.SendAsync<IEnumerable<TagNameDto>>(new RequestDto(cd.OTagsAPIUrl + $"/all")
        {
            ApiType = EApiType.GET,
        });
    }
}
