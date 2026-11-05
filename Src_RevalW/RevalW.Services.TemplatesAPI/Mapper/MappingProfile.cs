using AutoMapper;
using RevalW.Services.TemplatesAPI.Model;
using RevalW.Shared.Model;

namespace RevalW.Services.TemplatesAPI.Mapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Config, ConfigUpsertDto>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(tn => tn.Name)))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.Ignore());
        CreateMap<Config, ConfigHeaderDto>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(tn => tn.Name)))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.Ignore());
        CreateMap<Tag, ConfigTagDto>().ReverseMap();
        CreateMap<Tag, TagDto>();
        CreateMap<DocumentsResourceFile, DocumentsResourceFileDto>().ReverseMap();
        CreateMap<Document, DocumentTemplateDto>().ReverseMap();
        CreateMap<Document, DocumentHeaderDto>();
        CreateMap<Document, DocumentUpsertDto>();
        CreateMap<DynamicPropertyDto, NewItemProperty>().ReverseMap();
    }
}
