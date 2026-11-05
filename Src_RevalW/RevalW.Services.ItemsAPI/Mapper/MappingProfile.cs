using AutoMapper;
using RevalW.Services.ItemsAPI.Model;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;

namespace RevalW.Services.ItemsAPI.Mapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Item, ItemUpsertDto>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(tn => tn.Name)))
            .ForMember(dest => dest.ItemDataJson, opt => opt.MapFrom(src => src.ItemData.ToJson()))
            .ForMember(dest => dest.ConfigDataJson, opt => opt.MapFrom(src => src.ConfigData.ToJson()))
            .ForMember(dest => dest.DeviceDataJson, opt => opt.MapFrom(src => src.DeviceData.ToJson()))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.ItemData, opt => opt.Ignore())
            .ForMember(dest => dest.ConfigData, opt => opt.Ignore())
            .ForMember(dest => dest.DeviceData, opt => opt.Ignore());
        CreateMap<Item, ItemHeaderDto>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(tn => tn.Name)))
            .ForMember(dest => dest.GeneratedDocuments, opt => opt.MapFrom(src => src.GeneratedDocuments))
            .ForMember(dest => dest.ItemDataJson, opt => opt.MapFrom(src => src.ItemData.ToJson()))
            .ForMember(dest => dest.DeviceDataJson, opt => opt.MapFrom(src => src.DeviceData.ToJson()))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.GeneratedDocuments, opt => opt.MapFrom(src => src.GeneratedDocuments))
            .ForMember(dest => dest.ItemData, opt => opt.Ignore())
            .ForMember(dest => dest.ConfigData, opt => opt.Ignore())
            .ForMember(dest => dest.DeviceData, opt => opt.Ignore());
        CreateMap<Item, ItemAppHeaderDto>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(tn => tn.Name)))
            .ForMember(dest => dest.ItemDataJson, opt => opt.MapFrom(src => src.ItemData.ToJson()))
            .ForMember(dest => dest.DeviceDataJson, opt => opt.MapFrom(src => src.DeviceData.ToJson()))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.ItemData, opt => opt.Ignore())
            .ForMember(dest => dest.DeviceData, opt => opt.Ignore());
        CreateMap<Tag, TagNameDto>().ReverseMap();
        CreateMap<ComboSource, ComboSourceDto>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items.Select(i => i.Value)))
            .ReverseMap()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items.Select(i => new ComboItem { Value = i })));
        CreateMap<CommonDataWrapper, CommonDataWrapperDto>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items.ToDictionary(i => i.KeyValue, i => i.Data)))
            .ReverseMap()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items.Select(i => new CommonDataItem { KeyValue = i.Key, Data = i.Value })));
        CreateMap<GeneratedDocumentDto, GeneratedDocument>().ReverseMap();
    }
}
