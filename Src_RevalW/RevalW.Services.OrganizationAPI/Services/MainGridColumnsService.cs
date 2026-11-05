using AutoMapper;
using RevalW.Services.OrganizationAPI.Data;
using RevalW.Services.OrganizationAPI.Extensions;
using RevalW.Services.OrganizationAPI.Model;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;

namespace RevalW.Services.OrganizationAPI.Services;

public partial class MainGridColumnsService(AppDbContext db, IMapper mapper)
{
    public async Task<(int status, ResponseDto<IEnumerable<MainGridColumnDto>> response)> Insert(long currentOrgId, bool isCreator, bool isUberAdmin, List<MainGridColumnDto> dto)
    {
        if (!isUberAdmin && !isCreator)
            return (StatusCodes.Status400BadRequest, ResponseDto<IEnumerable<MainGridColumnDto>>.Err("have no permission"));
        foreach (var col in dto)
        {
            if (!string.IsNullOrWhiteSpace(col.Name)) continue;
            return (StatusCodes.Status400BadRequest, ResponseDto<IEnumerable<MainGridColumnDto>>.Err("name and label cannot be empty"));
        }
        foreach (var existing in db.ItemGridColumns.Where(c => c.OrganizationId == currentOrgId))
        {
            db.ItemGridColumns.Remove(existing);
        }
        foreach (var col in dto)
        {
            var entity = mapper.Map<ItemGridColumn>(col);
            entity.OrganizationId = currentOrgId;
            db.ItemGridColumns.Add(entity);
        }
        await db.SaveChangesAsync();
        var ret = db.ItemGridColumns.Where(c => c.OrganizationId == currentOrgId).ToList();
        return (StatusCodes.Status200OK, ResponseDto<IEnumerable<MainGridColumnDto>>.Ok(ret.Select(c => c.ToMainGridColumnDto())));
    }
    public (int status, ResponseDto<IEnumerable<MainGridColumnDto>> response) GetAll(long currentOrgId)
    {
        var ret = db.ItemGridColumns.Where(c => c.OrganizationId == currentOrgId).ToList();
        return (StatusCodes.Status200OK, ResponseDto<IEnumerable<MainGridColumnDto>>.Ok(ret.Select(c => c.ToMainGridColumnDto())));
    }
}
