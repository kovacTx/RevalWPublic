using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RevalW.Services.ItemsAPI.Data;
using RevalW.Services.ItemsAPI.Model;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;

namespace RevalW.Services.ItemsAPI.Services;

public class CommonDataService(AppDbContext db, IMapper mapper)
{
    public async Task<(int status, ResponseDto<CommonDataWrapperDto> response)> UpsertCommonDataWrapper(long currentOrgId, CommonDataWrapperDto dto)
    {
        try
        {
            if (string.IsNullOrEmpty(dto.Name)) return (StatusCodes.Status400BadRequest, ResponseDto<CommonDataWrapperDto>.Err("the commonData name cannot be default"));
            var toUpdate = await db.CommonDataWrappers.Include(c => c.Items).FirstOrDefaultAsync(cs => cs.OrganizationId == currentOrgId && cs.ConfigId == dto.ConfigId && cs.Name == dto.Name);

            if (toUpdate is null) toUpdate = await InsertNewCommonDataWrapper(currentOrgId, dto);
            else toUpdate = UpdateCommonDataWrapper(toUpdate, dto);
            await db.SaveChangesAsync();

            var ret = mapper.Map<CommonDataWrapperDto>(toUpdate);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<CommonDataWrapperDto>.Err("could not read updated comboSource"));
            return (StatusCodes.Status200OK, ResponseDto<CommonDataWrapperDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<CommonDataWrapperDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ComboSourceDto> response)> UpsertCombo(long currentOrgId, ComboSourceDto dto)
    {
        try
        {
            if (string.IsNullOrEmpty(dto.Name)) return (StatusCodes.Status400BadRequest, ResponseDto<ComboSourceDto>.Err("the comboSource name cannot be default"));
            var toUpdate = await db.ComboSources.Include(c => c.Items).FirstOrDefaultAsync(cs => cs.OrganizationId == currentOrgId && cs.ConfigId == dto.ConfigId && cs.Name == dto.Name);

            if (toUpdate is null) toUpdate = await InsertNewComboSource(currentOrgId, dto);
            else toUpdate = UpdateComboSource(toUpdate, dto);
            await db.SaveChangesAsync();

            var ret = mapper.Map<ComboSourceDto>(toUpdate);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<ComboSourceDto>.Err("could not read updated comboSource"));
            return (StatusCodes.Status200OK, ResponseDto<ComboSourceDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ComboSourceDto>.Err(e.ToString()));
        }
    }
    public (int status, ResponseDto<List<ComboSourceDto>> response) GetCombosByConfigId(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var source = db.ComboSources.Include(c => c.Items).Where(c => c.ConfigId == id && (isUber || c.OrganizationId == currentOrgId));
            if (source is null) return (StatusCodes.Status404NotFound, ResponseDto<List<ComboSourceDto>>.Err("source with this config id does not exists"));
            var ret = mapper.Map<List<ComboSourceDto>>(source);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<List<ComboSourceDto>>.Err("could not read comboSources"));
            return (StatusCodes.Status200OK, ResponseDto<List<ComboSourceDto>>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<List<ComboSourceDto>>.Err(e.ToString()));
        }
    }
    public (int status, ResponseDto<List<CommonDataWrapperDto>> response) GetComonDataWrappersByConfigId(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var source = db.CommonDataWrappers.Include(c => c.Items).Where(c => c.ConfigId == id && (isUber || c.OrganizationId == currentOrgId));
            if (source is null) return (StatusCodes.Status404NotFound, ResponseDto<List<CommonDataWrapperDto>>.Err("source with this config id does not exists"));
            var ret = mapper.Map<List<CommonDataWrapperDto>>(source);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<List<CommonDataWrapperDto>>.Err("could not read commonData"));
            return (StatusCodes.Status200OK, ResponseDto<List<CommonDataWrapperDto>>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<List<CommonDataWrapperDto>>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> DeleteCombo(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var source = await db.ComboSources.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id && (isUber || c.OrganizationId == currentOrgId));
            if (source is null) return (StatusCodes.Status404NotFound, ResponseDtoo.Err("source with this id does not exists"));
            db.ComboSources.Remove(source);
            await db.SaveChangesAsync();
            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> DeleteCommonData(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var source = await db.CommonDataWrappers.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id && (isUber || c.OrganizationId == currentOrgId));
            if (source is null) return (StatusCodes.Status404NotFound, ResponseDtoo.Err("common data with this id does not exists"));
            db.CommonDataWrappers.Remove(source);
            await db.SaveChangesAsync();
            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }


    private async Task<CommonDataWrapper> InsertNewCommonDataWrapper(long orgId, CommonDataWrapperDto dto)
    {
        var commonDataWrapper = mapper.Map<CommonDataWrapper>(dto);
        commonDataWrapper.OrganizationId = orgId;
        db.CommonDataWrappers.Add(commonDataWrapper);
        await db.SaveChangesAsync();
        return commonDataWrapper;
    }
    private CommonDataWrapper UpdateCommonDataWrapper(CommonDataWrapper existing, CommonDataWrapperDto dto)
    {
        existing.Name = dto.Name ?? existing.Name;
        var incoming = dto.Items?.Select(s => s.Key.Trim())
                                 .ToList() ?? [];
        var toRemove = existing.Items.Where(i => !incoming.Contains(i.KeyValue)).ToList();
        foreach (var rem in toRemove)
        {
            existing.Items.Remove(rem);
            db.CommonDataItems.Remove(rem);
        }
        var existingValues = new HashSet<string?>(existing.Items.Select(i => i.KeyValue));
        foreach (var val in incoming)
        {
            string? data;
            if (existingValues.Contains(val))
            {
                var existingItem = existing.Items.FirstOrDefault(i => i.KeyValue == val);
                if (existingItem is null) continue;
                if (dto.Items?.TryGetValue(val, out data) != true) data = null;
                existingItem.Data = data;
            }
            else
            {
                if (dto.Items?.TryGetValue(val, out data) != true) data = null;
                var newItem = new CommonDataItem { CommonDataWrapperId = existing.Id, KeyValue = val, Data = data };
                existing.Items.Add(newItem);
            }
        }
        return existing;
    }
    private async Task<ComboSource> InsertNewComboSource(long orgId, ComboSourceDto dto)
    {
        var comboSource = mapper.Map<ComboSource>(dto);
        comboSource.OrganizationId = orgId;
        db.ComboSources.Add(comboSource);
        await db.SaveChangesAsync();
        return comboSource;
    }
    private ComboSource UpdateComboSource(ComboSource existing, ComboSourceDto dto)
    {
        existing.Name = dto.Name ?? existing.Name;
        var incoming = dto.Items?.Where(s => !string.IsNullOrWhiteSpace(s))
                                 .Select(s => s.Trim())
                                 .ToList() ?? [];
        var toRemove = existing.Items.Where(i => !incoming.Contains(i.Value)).ToList();
        foreach (var rem in toRemove)
        {
            existing.Items.Remove(rem);
            db.ComboItems.Remove(rem);
        }
        var existingValues = new HashSet<string>(existing.Items.Select(i => i.Value));
        foreach (var val in incoming)
        {
            if (existingValues.Contains(val)) continue;
            var newItem = new ComboItem { ComboSourceId = existing.Id, Value = val };
            existing.Items.Add(newItem);
        }
        return existing;
    }
}
