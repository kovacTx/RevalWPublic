using RevalW.App.Service.IFace;
using RevalW.App.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;

namespace RevalW.App.Service;

public class ConfigsTagsService(IBaseService baseService, CommonData cd)
{
    public async Task<ResponseDto<TagNameDto?>?> CreateAsync(TagNameDto dto)
    {
        return await baseService.SendAsync<TagNameDto>(new RequestDto(cd.CTagsAPIUrl + "/create")
        {
            ApiType = EApiType.POST,
            Data = dto,
        });
    }
    public async Task<ResponseDto<IEnumerable<TagDto>?>?> GetAllAsync(bool? deleted)
    {
        var path = "/all";
        if (deleted.HasValue) path += $"?deleted={deleted}";

        return await baseService.SendAsync<IEnumerable<TagDto>>(new RequestDto(cd.CTagsAPIUrl + path)
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<TagNameDto?>?> GetByIdAsync(int id)
    {
        return await baseService.SendAsync<TagNameDto?>(new RequestDto(cd.CTagsAPIUrl + $"/byId?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<TagNameDto?>?> GetByName(string name)
    {
        return await baseService.SendAsync<TagNameDto?>(new RequestDto(cd.CTagsAPIUrl + $"/byName?name={name}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<TagNameDto?>?> UpdateAsync(TagNameDto dto)
    {
        return await baseService.SendAsync<TagNameDto>(new RequestDto(cd.CTagsAPIUrl + "/update")
        {
            ApiType = EApiType.PUT,
            Data = dto,
        });
    }
    public async Task<ResponseDtoo?> DeleteConfigAsync(long id)
    {
        return await baseService.SendAsync(new RequestDto(cd.CTagsAPIUrl + $"/delete?id={id}")
        {
            ApiType = EApiType.DELETE,
        });
    }

}
