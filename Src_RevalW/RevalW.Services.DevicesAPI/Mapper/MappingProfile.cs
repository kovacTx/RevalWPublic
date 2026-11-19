using AutoMapper;
using RevalW.Services.DevicesAPI.Model;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;

namespace RevalW.Services.DevicesAPI.Mapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Device, DeviceUpsertDto>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(tn => tn.Name)))
            .ForMember(dest => dest.DeviceDataJson, opt => opt.MapFrom(src => src.DeviceData.ToJson()))
            .ForMember(dest => dest.ConfigDataJson, opt => opt.MapFrom(src => src.ConfigData.ToJson()))
            .ForMember(dest => dest.OwnerEmailsJson, opt => opt.MapFrom(src => src.OwnerEmails.ToJson()))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.DeviceData, opt => opt.Ignore())
            .ForMember(dest => dest.ConfigData, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerEmails, opt => opt.Ignore());
        CreateMap<Device, DeviceHeaderDto>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(tn => tn.Name)))
            .ForMember(dest => dest.GeneratedDocuments, opt => opt.MapFrom(src => src.GeneratedDocuments))
            .ForMember(dest => dest.DeviceDataJson, opt => opt.MapFrom(src => src.DeviceData.ToJson()))
            .ForMember(dest => dest.OwnerEmailsJson, opt => opt.MapFrom(src => src.OwnerEmails.ToJson()))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.GeneratedDocuments, opt => opt.MapFrom(src => src.GeneratedDocuments))
            .ForMember(dest => dest.DeviceData, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerEmails, opt => opt.Ignore());
        CreateMap<Device, DeviceAppHeaderDto>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(tn => tn.Name)))
            .ForMember(dest => dest.DeviceDataJson, opt => opt.MapFrom(src => src.DeviceData.ToJson()))
            .ForMember(dest => dest.OwnerEmailsJson, opt => opt.MapFrom(src => src.OwnerEmails.ToJson()))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.DeviceData, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerEmails, opt => opt.Ignore());
        CreateMap<Tag, TagNameDto>().ReverseMap();
    }
}
