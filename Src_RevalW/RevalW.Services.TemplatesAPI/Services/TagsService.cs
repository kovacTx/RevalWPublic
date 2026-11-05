using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RevalW.Services.TemplatesAPI.Data;
using RevalW.Services.TemplatesAPI.Model;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;

namespace RevalW.Services.TemplatesAPI.Services;

public class TagsService(AppDbContext db, IMapper mapper)
{
    public async Task<(int status, ResponseDto<IEnumerable<TagDto>> response)> GetAll(long currentOrgId, bool isUber, bool? deleted)
    {
        try
        {
            var tns = db.Tags
                .Include(t => t.Configs)
                .Where(t => isUber || t.OrganizationId == currentOrgId)
                .Where(t => !deleted.HasValue || t.Deleted == deleted.Value);
            if (tns is null) return (StatusCodes.Status404NotFound, ResponseDto<IEnumerable<TagDto>>.Err("could not read saved Tag"));
            var ret = mapper.Map<IEnumerable<TagDto>>(tns.ToList());
            if (ret is null) return (StatusCodes.Status404NotFound, ResponseDto<IEnumerable<TagDto>>.Err("could not read saved Tag"));
            foreach (var tDto in ret)
                tDto.Id = (await tns.FirstOrDefaultAsync(t => t.Id == tDto.Id))?.Configs?.FirstOrDefault()?.Id ?? 0;
            return (StatusCodes.Status200OK, ResponseDto<IEnumerable<TagDto>>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<IEnumerable<TagDto>>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigTagDto> response)> Create(long currentOrgId, bool isUber, ConfigTagDto dto)
    {
        try
        {
            var (status, response) = await CreateAndReturnTag(currentOrgId, isUber, dto);
            if (status != StatusCodes.Status200OK) return (status, ResponseDto<ConfigTagDto>.Err(response.Message ?? "failed to create Tag"));

            var dtoToRet = mapper.Map<ConfigTagDto>(response.Result);
            if (dtoToRet is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigTagDto>.Err("could not read saved Tag"));
            return (StatusCodes.Status200OK, ResponseDto<ConfigTagDto>.Ok(dtoToRet));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigTagDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<Tag> response)> CreateAndReturnTag(long currentOrgId, bool isUber, ConfigTagDto dto)
    {
        if (dto.Id != default) return (StatusCodes.Status400BadRequest, ResponseDto<Tag>.Err("the id have to be 0"));
        if (!isUber || dto.OrganizationId == default) dto.OrganizationId = currentOrgId;

        var lowerName = dto.Name.ToLower();
        if (db.Tags.Any(t => t.OrganizationId == currentOrgId && t.Name.ToLower() == lowerName)) return (StatusCodes.Status400BadRequest, ResponseDto<Tag>.Err("the Tag with this name already exists"));

        var ret = mapper.Map<Tag>(dto);
        if (ret is null) return (StatusCodes.Status400BadRequest, ResponseDto<Tag>.Err("cannot read Tag to save"));
        db.Tags.Add(ret);
        await db.SaveChangesAsync();

        return (StatusCodes.Status200OK, ResponseDto<Tag>.Ok(ret));
    }
    public async Task<(int status, ResponseDto<ConfigTagDto> response)> GetByName(long currentOrgId, bool isUber, string name)
    {
        try
        {
            var retEntity = await GetEntityByName(currentOrgId, isUber, name);
            if (retEntity is null) return (StatusCodes.Status404NotFound, ResponseDto<ConfigTagDto>.Err("no Tag with this name exists"));

            var ret = mapper.Map<ConfigTagDto>(retEntity);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigTagDto>.Err("cannot read Tag"));
            return (StatusCodes.Status200OK, ResponseDto<ConfigTagDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigTagDto>.Err(e.ToString()));
        }
    }
    public Task<Tag?> GetEntityByName(long currentOrgId, bool isUber, string name)
    {
        var lowerName = name?.ToLower();
        return db.Tags.FirstOrDefaultAsync(i => i.Name.ToLower() == lowerName && (isUber || i.OrganizationId == currentOrgId));
    }
    public async Task<(int status, ResponseDto<ConfigTagDto> response)> GetById(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var tn = await db.Tags.FindAsync(id);
            if (tn is null) return (StatusCodes.Status404NotFound, ResponseDto<ConfigTagDto>.Err("no Tag with this id exists"));
            if (tn.OrganizationId != currentOrgId && !isUber) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigTagDto>.Err("have no rights to fetch this Tag"));

            var ret = mapper.Map<ConfigTagDto>(tn);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigTagDto>.Err("cannot read Tag"));
            return (StatusCodes.Status200OK, ResponseDto<ConfigTagDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigTagDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigTagDto> response)> Update(long currentOrgId, bool isUber, ConfigTagDto dto)
    {

        try
        {
            if (dto.Id == default) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigTagDto>.Err("the id cannot be 0"));
            var toUpdate = await db.Tags.FindAsync(dto.Id);
            if (toUpdate is null) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigTagDto>.Err("there is no such Tag to update"));
            if (isUber) dto.OrganizationId = toUpdate.OrganizationId;
            else dto.OrganizationId = currentOrgId;

            if (toUpdate.OrganizationId != dto.OrganizationId) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigTagDto>.Err("cannot change the organization"));

            var newEntity = mapper.Map<Tag>(dto);
            if (newEntity is null) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigTagDto>.Err("invalid Tag to update"));

            db.Tags.Entry(toUpdate).State = EntityState.Detached;
            db.Tags.Update(newEntity);
            await db.SaveChangesAsync();
            var ret = mapper.Map<ConfigTagDto>(newEntity);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigTagDto>.Err("could not read updated Tag"));
            return (StatusCodes.Status200OK, ResponseDto<ConfigTagDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigTagDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigTagDto> response)> MarkAsDeleted(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var toDelete = await db.Tags.FindAsync(id);
            if (toDelete is null) return (StatusCodes.Status404NotFound, ResponseDto<ConfigTagDto>.Err("cannot delete Tag that does not exists"));
            if (!isUber && toDelete.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigTagDto>.Err("cannot delete Tag that does not belongs to this organization"));
            if (toDelete.Deleted) return (StatusCodes.Status200OK, ResponseDto<ConfigTagDto>.Ok());

            toDelete.Deleted = true;
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDto<ConfigTagDto>.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigTagDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ConfigTagDto> response)> MarkAsUndeleted(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var toUnDelete = await db.Tags.FindAsync(id);
            if (toUnDelete is null) return (StatusCodes.Status404NotFound, ResponseDto<ConfigTagDto>.Err("cannot undelete Tag that does not exists"));
            if (!isUber && toUnDelete.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<ConfigTagDto>.Err("cannot undelete Tag that does not belongs to this organization"));
            if (!toUnDelete.Deleted) return (StatusCodes.Status200OK, ResponseDto<ConfigTagDto>.Ok());

            toUnDelete.Deleted = false;
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDto<ConfigTagDto>.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ConfigTagDto>.Err(e.ToString()));
        }
    }
}
