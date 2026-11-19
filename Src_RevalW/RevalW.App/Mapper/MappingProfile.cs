using AutoMapper;
using RevalW.App.Model;
using RevalW.Shared.Model;
using System.Text.Json;
using SJsonSerializer = RevalM.SharedStorage.Utils.JsonSerializer;

namespace RevalW.App.Mapper;

public class MappingProfile : Profile
{
    private static readonly JsonSerializerOptions serializerOptions = new() { PropertyNameCaseInsensitive = true };
    public MappingProfile()
    {
        CreateMap<ConfigUpsertDto, InnerConfigUpsertDto>()
            .ForMember(dest => dest.TagsJson, opt => opt.MapFrom(src => SerializeObject(src.Tags)))
            .ForMember(dest => dest.NewItemPropertiesJson, opt => opt.MapFrom(src => SerializeObject(src.NewItemProperties)))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => DeserializeTags(src.TagsJson)))
            .ForMember(dest => dest.NewItemProperties, opt => opt.MapFrom(src => DeserializeDynamicProperties(src.NewItemPropertiesJson)));
        CreateMap<InnerConfigUpsertDto, UpdateImageDto>();
        CreateMap<InnerConfigUpsertDto, UpdateDllDto>();
        CreateMap<ItemUpsertDto, InnerItemUpsertDto>().ReverseMap();
        CreateMap<DeviceUpsertDto, InnerDeviceUpsertDto>()
            .ForMember(dest => dest.TagsJson, opt => opt.MapFrom(src => SerializeObject(src.Tags)))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => DeserializeTags(src.TagsJson)));
        CreateMap<OrganizationDto, InnerOrganizationUpdateDto>()
            .ForMember(dest => dest.TagsJson, opt => opt.MapFrom(src => SerializeObject(src.Tags)))
            .ForMember(dest => dest.NewUserPropertiesJson, opt => opt.MapFrom(src => SerializeObject(src.NewUserProperties)))
            .ForMember(dest => dest.NewItemPropertiesJson, opt => opt.MapFrom(src => SerializeObject(src.NewItemProperties)))
            .ForMember(dest => dest.RolesJson, opt => opt.MapFrom(src => SerializeObject(src.Roles)))
            .ForMember(dest => dest.NewDevicePropertiesJson, opt => opt.MapFrom(src => SerializeObject(src.NewDeviceProperties)))
            .ForMember(dest => dest.ItemGridColumnsJson, opt => opt.MapFrom(src => SerializeObject(src.ItemGridColumns)))
            .ForMember(dest => dest.DeviceGridColumnsJson, opt => opt.MapFrom(src => SerializeObject(src.DeviceGridColumns)))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => DeserializeTags(src.TagsJson)))
            .ForMember(dest => dest.NewUserProperties, opt => opt.MapFrom(src => DeserializeDynamicProperties(src.NewUserPropertiesJson)))
            .ForMember(dest => dest.NewItemProperties, opt => opt.MapFrom(src => DeserializeDynamicProperties(src.NewItemPropertiesJson)))
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => DeserializeRoles(src.RolesJson)))
            .ForMember(dest => dest.NewDeviceProperties, opt => opt.MapFrom(src => DeserializeDynamicProperties(src.NewDevicePropertiesJson)))
            .ForMember(dest => dest.ItemGridColumns, opt => opt.MapFrom(src => DeserializeMainGridColumns(src.ItemGridColumnsJson)))
            .ForMember(dest => dest.DeviceGridColumns, opt => opt.MapFrom(src => DeserializeMainGridColumns(src.DeviceGridColumnsJson)));
    }
    public static string? SerializeObject(object value)
        => value == null ? null : JsonSerializer.Serialize(value);
    private static List<string> DeserializeTags(string? value)
    {
        if (string.IsNullOrEmpty(value)) return [];

        try
        {
            return SJsonSerializer.Deserialize<List<string>>(value, serializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
    public static List<TagDto> DeserializeTagsDto(string? value)
    {
        if (string.IsNullOrEmpty(value)) return [];

        try
        {
            return SJsonSerializer.Deserialize<List<TagDto>>(value, serializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
    public static List<DocumentTemplateDto> DeserializePrintableDocuments(string? value)
    {
        if (string.IsNullOrEmpty(value)) return [];

        try
        {
            return SJsonSerializer.Deserialize<List<DocumentTemplateDto>>(value, serializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
    public static List<DocumentsResourceFileDto> DeserializeDocumentsResources(string? value)
    {
        if (string.IsNullOrEmpty(value)) return [];

        try
        {
            return SJsonSerializer.Deserialize<List<DocumentsResourceFileDto>>(value, serializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
    private static List<DynamicPropertyDto> DeserializeDynamicProperties(string? value)
    {
        if (string.IsNullOrEmpty(value)) return [];

        try
        {
            return SJsonSerializer.Deserialize<List<DynamicPropertyDto>>(value, serializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
    private static List<RoleDto> DeserializeRoles(string? value)
    {
        if (string.IsNullOrEmpty(value)) return [];

        try
        {
            return SJsonSerializer.Deserialize<List<RoleDto>>(value, serializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
    private static List<MainGridColumnDto> DeserializeMainGridColumns(string? value)
    {
        if (string.IsNullOrEmpty(value)) return [];

        try
        {
            return SJsonSerializer.Deserialize<List<MainGridColumnDto>>(value, serializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
