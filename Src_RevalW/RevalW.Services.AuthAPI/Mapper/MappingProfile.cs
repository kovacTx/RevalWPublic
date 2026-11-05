using AutoMapper;
using RevalW.Services.AuthAPI.Model;
using RevalW.Shared.Model;

namespace RevalW.Services.AuthAPI.Mapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ApplicationUser, UserDto>().ReverseMap();
        CreateMap<RegistrationWithinOrganizationRequestDto, UserRegistrationRequest>();
        CreateMap<UserRegistrationRequest, ApplicationUser>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.NormalizedEmail, opt => opt.MapFrom(src => src.Email.ToUpper()));
    }
}
