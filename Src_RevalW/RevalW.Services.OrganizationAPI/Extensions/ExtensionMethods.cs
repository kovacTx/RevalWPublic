using AutoMapper;
using RevalW.Services.OrganizationAPI.Model;
using RevalW.Services.Shared.Model;
using RevalW.Services.Shared.Utils;
using RevalW.Shared.Model;

namespace RevalW.Services.OrganizationAPI.Extensions;

public static class ExtensionMethods
{
    private static IMapper? _mapper;
    public static IMapper Mapper => _mapper ??= ServiceProviderAccessor.Instance.GetService<IMapper>()!;
    public static OrganizationDto ToOrganizationDto(this Organization dto)
        => Mapper.Map<OrganizationDto>(dto);
    public static MainGridColumnDto ToMainGridColumnDto(this MainGridColumn dto)
        => Mapper.Map<MainGridColumnDto>(dto);
}
