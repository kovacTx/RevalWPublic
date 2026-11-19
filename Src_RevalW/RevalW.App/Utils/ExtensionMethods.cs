using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using RevalW.App.Mapper;
using RevalW.App.Model;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;
using RevalW.Shared.Model.IFace;
using RevalW.Shared.Utils;

namespace RevalW.App.Utils;

public static class ExtensionMethods
{

    private static IMapper? _mapper;
    public static IMapper Mapper => _mapper ??= ServiceProviderAccessor.Instance.GetService<IMapper>()!;

    public static long CurrentOrgId(this RazorPageBase page)
    {
        if (!long.TryParse(page.User?.FindFirst(ClaimNames.OrgId)?.Value ?? string.Empty, out var ret))
            return -1;
        else
            return ret;
    }
    public static RedirectResult EndRedirectErr(this Controller controller, string url, string? message = null, IResponseDto? resp = null)
    {
        var haveBothMessages = !string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(resp?.Message);
        var messageToShow = message + (haveBothMessages ? ": " : "") + resp?.Message;
        if (!string.IsNullOrEmpty(messageToShow)) controller.TempData["error"] = messageToShow;
        return controller.Redirect(url);
    }
    public static RedirectResult EndRedirectOk(this Controller controller, string url, string? message = null)
    {
        if (!string.IsNullOrEmpty(message)) controller.TempData["success"] = message;
        return controller.Redirect(url);
    }
    public static ViewResult EndErr(this Controller controller, string? viewName = null, object? dto = null, string? message = null, IResponseDto? resp = null)
    {
        var haveBothMessages = !string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(resp?.Message);
        var messageToShow = message + (haveBothMessages ? ": " : "") + resp?.Message;
        if (!string.IsNullOrEmpty(messageToShow)) controller.TempData["error"] = messageToShow;
        return controller.View(viewName: viewName, model: dto);
    }
    public static ViewResult EndOk(this Controller controller, string? viewName = null, object? dto = null, string? message = null)
    {
        if (!string.IsNullOrEmpty(message)) controller.TempData["success"] = message;
        return controller.View(viewName, dto);
    }
    public static InnerConfigUpsertDto? ToInnerConfigUpsertDto(this ConfigUpsertDto? config, IEnumerable<DocumentTemplateDto>? documents = null, IEnumerable<DocumentsResourceFileDto>? documentResources = null)
    {
        if (config is null) return null;
        var ret = Mapper.Map<InnerConfigUpsertDto>(config);
        if (documents is not null) ret.PrintableDocumentsJson = MappingProfile.SerializeObject(documents);
        if (documentResources is not null) ret.OldDocumentsResourcesJson = MappingProfile.SerializeObject(documentResources);
        return ret;
    }
    public static ConfigUpsertDto? ToConfigUpsertDto(this InnerConfigUpsertDto? config)
    {
        if (config is null) return null;
        return Mapper.Map<ConfigUpsertDto>(config);
    }
    public static List<DocumentTemplateDto>? ToPrintableDocuments(this InnerConfigUpsertDto? config)
    {
        if (config is null) return null;
        return MappingProfile.DeserializePrintableDocuments(config.PrintableDocumentsJson);
    }
    public static List<DocumentsResourceFileDto>? ToDocumentsResources(this InnerConfigUpsertDto? config)
    {
        if (config is null) return null;
        return MappingProfile.DeserializeDocumentsResources(config.OldDocumentsResourcesJson);
    }
    public static InnerItemUpsertDto? ToInnerItemUpsertDto(this ItemUpsertDto? item)
    {
        if (item is null) return null;
        return Mapper.Map<InnerItemUpsertDto>(item);
    }
    public static InnerDeviceUpsertDto? ToInnerDeviceUpsertDto(this DeviceUpsertDto? item)
    {
        if (item is null) return null;
        return Mapper.Map<InnerDeviceUpsertDto>(item);
    }
    public static ItemUpsertDto? ToItemUpsertDto(this InnerItemUpsertDto? item)
    {
        if (item is null) return null;
        var ret = Mapper.Map<ItemUpsertDto>(item);
        var tagsDto = MappingProfile.DeserializeTagsDto(item.TagsJson);
        ret.Tags = tagsDto?.Select(t => t.Name)?.ToList() ?? [];
        return ret;
    }
    public static DeviceUpsertDto? ToDeviceUpsertDto(this InnerDeviceUpsertDto? item)
    {
        if (item is null) return null;
        var ret = Mapper.Map<DeviceUpsertDto>(item);
        var tagsDto = MappingProfile.DeserializeTagsDto(item.TagsJson);
        ret.Tags = tagsDto?.Select(t => t.Name)?.ToList() ?? [];
        return ret;
    }
    public static InnerOrganizationUpdateDto? ToInnerOrganizationUpdateDto(this OrganizationDto? item)
    {
        if (item is null) return null;
        return Mapper.Map<InnerOrganizationUpdateDto>(item);
    }
    public static OrganizationDto? ToOrganizationUpdateDto(this InnerOrganizationUpdateDto? item)
    {
        if (item is null) return null;
        return Mapper.Map<OrganizationDto>(item);
    }
    public static string[] ToAddresses(this List<PublicOrganizationTagDto>? tags)
        => tags?.SelectMany(t => t.Organizations.Select(o => $"{o.Street}, {o.City}, {o.CountryRegion}")).ToArray() ?? [];
}
