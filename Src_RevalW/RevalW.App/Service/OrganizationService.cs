using RevalW.App.Service.IFace;
using RevalW.App.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using RevalW.Shared.Utils;
using DTRequest = RevalW.Shared.Model.DataTablesRequest.Request;

namespace RevalW.App.Service;

public class OrganizationService(IBaseService baseService, CommonData cd)
{
    public async Task<ResponseDto<DataTablesResponse<OrganizationDto>?>?> GetByDataTablesAsync(DTRequest request)
    {
        return await baseService.SendAsync<DataTablesResponse<OrganizationDto>>(new RequestDto(cd.OrganizationAPIUrl + "/byDataTables")
        {
            ApiType = EApiType.POST,
            Data = request,
        });
    }
    public async Task<ResponseDto<OrganizationDto?>?> GetById(long id)
    {
        return await baseService.SendAsync<OrganizationDto>(new RequestDto(cd.OrganizationAPIUrl + $"/byId?id={id}")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<List<DynamicPropertyDto>?>?> GetNewItemProps()
    {
        return await baseService.SendAsync<List<DynamicPropertyDto>>(new RequestDto(cd.OrganizationAPIUrl + $"/newItemProperties")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<List<DynamicPropertyDto>?>?> GetNewDeviceProps()
    {
        return await baseService.SendAsync<List<DynamicPropertyDto>>(new RequestDto(cd.OrganizationAPIUrl + $"/newDeviceProperties")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<List<DynamicPropertyDto>?>?> GetNewUserProps()
    {
        return await baseService.SendAsync<List<DynamicPropertyDto>>(new RequestDto(cd.OrganizationAPIUrl + $"/newUserProperties")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<List<MainGridColumnDto>?>?> GetItemGridColumn()
    {
        return await baseService.SendAsync<List<MainGridColumnDto>>(new RequestDto(cd.OrganizationAPIUrl + $"/itemGridColumns")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<List<MainGridColumnDto>?>?> GetDeviceGridColumn()
    {
        return await baseService.SendAsync<List<MainGridColumnDto>>(new RequestDto(cd.OrganizationAPIUrl + $"/deviceGridColumns")
        {
            ApiType = EApiType.GET,
        });
    }
    public async Task<ResponseDto<OrganizationDto?>?> UpdateAsync(OrganizationDto dto)
    {
        return await baseService.SendAsync<OrganizationDto>(new RequestDto(cd.OrganizationAPIUrl + "/update")
        {
            ApiType = EApiType.PUT,
            Data = dto,
        });
    }
    public async Task<ResponseDto<List<PublicOrganizationTagDto>?>?> GetOrganizationsByTags(string tags)
    {
        return await baseService.SendAsync<List<PublicOrganizationTagDto>>(new RequestDto(cd.OTagsAPIUrl + $"/organizationsByTags?tags={tags}")
        {
            ApiType = EApiType.GET,
        });
    }
}
