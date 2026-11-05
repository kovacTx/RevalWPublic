using AutoMapper;
using RevalW.Services.OrganizationAPI.Model;
using RevalW.Services.Shared.Model;
using RevalW.Shared.Model;

namespace RevalW.Services.OrganizationAPI.Mapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Organization, OrganizationDto>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(t => t.Name)))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.Ignore());
        CreateMap<Organization, PublicOrganizationDto>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(t => t.Name)));
        CreateMap<Tag, PublicOrganizationTagDto>().ReverseMap();
        CreateMap<Tag, TagNameDto>();
        CreateMap<MainGridColumnDto, ItemGridColumn>().ReverseMap();
        CreateMap<MainGridColumnDto, DeviceGridColumn>().ReverseMap();
        CreateMap<DynamicProperty, DynamicPropertyDto>().ReverseMap();
        CreateMap<DynamicPropertyDto, NewUserProperty>().ReverseMap();
        CreateMap<DynamicPropertyDto, NewDeviceProperty>().ReverseMap();
        CreateMap<DynamicPropertyDto, NewItemProperty>().ReverseMap();
    }
}
