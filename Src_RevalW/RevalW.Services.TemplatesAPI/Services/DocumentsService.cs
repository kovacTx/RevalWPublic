using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RevalW.Services.TemplatesAPI.Data;
using RevalW.Services.TemplatesAPI.Model;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;

namespace RevalW.Services.TemplatesAPI.Services;

public class DocumentsService(AppDbContext db, IMapper mapper, FileStorageService fileStorageService)
{
    public async Task<(int status, ResponseDto<DocumentTemplateDto> response)> Create(long currentOrgId, bool isUber, DocumentTemplateDto dto)
    {
        try
        {
            if (dto.Id != default) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentTemplateDto>.Err("the id have to be 0"));
            if (!isUber || dto.OrganizationId == default) dto.OrganizationId = currentOrgId;

            var document = mapper.Map<Document>(dto);
            if (document is null) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentTemplateDto>.Err("cannot read Document to save"));
            document.LastUpdated = DateTime.UtcNow;

            db.Documents.Add(document);
            await db.SaveChangesAsync();
            var ret = mapper.Map<DocumentTemplateDto>(document);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentTemplateDto>.Err("could not read saved Document"));
            return (StatusCodes.Status200OK, ResponseDto<DocumentTemplateDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentTemplateDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<DocumentTemplateDto> response)> GetById(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var document = await db.Documents.FirstOrDefaultAsync(c => c.Id == id && (isUber || c.OrganizationId == currentOrgId));
            if (document is null) return (StatusCodes.Status404NotFound, ResponseDto<DocumentTemplateDto>.Err("Document with this id does not exists"));
            var ret = mapper.Map<DocumentTemplateDto>(document);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentTemplateDto>.Err("could not read Document"));
            return (StatusCodes.Status200OK, ResponseDto<DocumentTemplateDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentTemplateDto>.Err(e.ToString()));
        }
    }
    public (int status, ResponseDto<IEnumerable<DocumentTemplateDto>> response) GetByConfigId(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var documents = db.Documents.Where(c => c.ConfigId == id && !c.Deleted && (isUber || c.OrganizationId == currentOrgId));
            if (documents is null) return (StatusCodes.Status404NotFound, ResponseDto<IEnumerable<DocumentTemplateDto>>.Err("Documents with this configs id does not exists"));
            var ret = mapper.Map<IEnumerable<DocumentTemplateDto>>(documents);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<IEnumerable<DocumentTemplateDto>>.Err("could not read Document"));
            return (StatusCodes.Status200OK, ResponseDto<IEnumerable<DocumentTemplateDto>>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<IEnumerable<DocumentTemplateDto>>.Err(e.ToString()));
        }
    }
    public (int status, ResponseDto<IEnumerable<DocumentHeaderDto>> response) GetHeadersByConfigId(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var documents = db.Documents.Where(c => c.ConfigId == id && !c.Deleted && (isUber || c.OrganizationId == currentOrgId));
            if (documents is null) return (StatusCodes.Status404NotFound, ResponseDto<IEnumerable<DocumentHeaderDto>>.Err("Documents for this configs id does not exists"));
            var ret = mapper.Map<IEnumerable<DocumentHeaderDto>>(documents);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<IEnumerable<DocumentHeaderDto>>.Err("could not read Documents"));
            return (StatusCodes.Status200OK, ResponseDto<IEnumerable<DocumentHeaderDto>>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<IEnumerable<DocumentHeaderDto>>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<DocumentTemplateDto> response)> Update(long currentOrgId, bool isUber, DocumentTemplateDto dto)
    {
        try
        {
            if (dto.Id == default) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentTemplateDto>.Err("the id cannot be 0"));
            var toUpdate = await db.Documents.FirstOrDefaultAsync(tn => tn.Id == dto.Id);
            if (toUpdate is null) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentTemplateDto>.Err("there is no such Document to update"));
            if (isUber) dto.OrganizationId = toUpdate.OrganizationId;
            else dto.OrganizationId = currentOrgId;

            if (toUpdate.OrganizationId != dto.OrganizationId) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentTemplateDto>.Err("cannot change the organization"));

            var item = mapper.Map<Document>(dto);
            if (item is null) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentTemplateDto>.Err("invalid Document to update"));

            db.Documents.Entry(toUpdate).CurrentValues.SetValues(item); // update scalar fields

            toUpdate.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var ret = mapper.Map<DocumentTemplateDto>(toUpdate);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentTemplateDto>.Err("could not read updated Document"));
            return (StatusCodes.Status200OK, ResponseDto<DocumentTemplateDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentTemplateDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<DocumentUpsertDto> response)> Autosave(long currentOrgId, bool isUber, MonacoAutosaveRequest dto)
    {
        try
        {
            if (dto.Id == default) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentUpsertDto>.Err("the id cannot be 0"));
            var toUpdate = await db.Documents.FirstOrDefaultAsync(tn => tn.Id == dto.Id);
            if (toUpdate is null) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentUpsertDto>.Err("there is no such Document to update"));
            if (toUpdate.ConfigId != dto.ConfigId) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentUpsertDto>.Err("we can not Autosave Document that does not belong to this Config"));
            if (isUber) currentOrgId = toUpdate.OrganizationId;

            if (toUpdate.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentUpsertDto>.Err("cannot change the organization (or update the Document that does not belongs to this organization)"));

            toUpdate.Template = dto.Content ?? "";
            toUpdate.LastUpdated = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var ret = mapper.Map<DocumentUpsertDto>(toUpdate);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentUpsertDto>.Err("could not read updated Config"));
            return (StatusCodes.Status200OK, ResponseDto<DocumentUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentUpsertDto>.Err(e.ToString()));
        }
    }
    public (int status, ResponseDto<List<DocumentsResourceFileDto>?> response) GetResourcesByConfigId(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var resources = db.DocumentsResourceFiles.Where(c => c.ConfigId == id && (isUber || c.OrganizationId == currentOrgId));
            if (resources is null) return (StatusCodes.Status404NotFound, ResponseDto<List<DocumentsResourceFileDto>?>.Err("Document resources for this configs id does not exists"));
            var ret = mapper.Map<List<DocumentsResourceFileDto>?>(resources);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<List<DocumentsResourceFileDto>?>.Err("could not read Document resources"));
            return (StatusCodes.Status200OK, ResponseDto<List<DocumentsResourceFileDto>?>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<List<DocumentsResourceFileDto>?>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<DocumentsResourceFileDto?> response)> UploadResourceFile(long currentOrgId, bool isUber, UploadDocumentsResourceFileDto dto)
    {
        try
        {
            if (dto.File is null) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentsResourceFileDto?>.Err("no file?"));
            var toUpdate = await db.Configs.FirstOrDefaultAsync(c => c.Id == dto.ConfigId);
            if (toUpdate is null) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentsResourceFileDto?>.Err("there is no such Config where to upload file"));
            if (isUber) currentOrgId = toUpdate.OrganizationId;
            if (toUpdate.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentsResourceFileDto?>.Err("cannot upload resources to config that does not belongs to this organization"));
            if (isUber) currentOrgId = dto.OrganizationId;
            if (dto.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<DocumentsResourceFileDto?>.Err("cannot upload resources to config that does not belongs to this organization"));

            var fBlobPath = await fileStorageService.UploadResourceFile(dto);
            if (string.IsNullOrEmpty(fBlobPath)) return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentsResourceFileDto?>.Err("failed to upload file"));
            var resourceFile = new DocumentsResourceFile
            {
                ConfigId = dto.ConfigId,
                OrganizationId = dto.OrganizationId,
                Name = dto.File.FileName,
                PrivatePath = fBlobPath
            };
            db.DocumentsResourceFiles.Add(resourceFile);
            await db.SaveChangesAsync();

            var ret = mapper.Map<DocumentsResourceFileDto>(resourceFile);
            if (ret is null) return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentsResourceFileDto?>.Err("could not read updated Config"));
            return (StatusCodes.Status200OK, ResponseDto<DocumentsResourceFileDto?>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<DocumentsResourceFileDto?>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDtoo response)> RemoveResourceFile(long currentOrgId, bool isUber, long id)
    {
        try
        {
            if (id == 0) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("the id cannot be 0"));
            var toRemove = await db.DocumentsResourceFiles.FirstOrDefaultAsync(dr => dr.Id == id);
            if (toRemove is null) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("there is no such Resource file to remove"));
            if (isUber) currentOrgId = toRemove.OrganizationId;
            if (toRemove.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("cannot remove resources that does not belongs to this organization"));
            if (string.IsNullOrEmpty(toRemove.PrivatePath)) return (StatusCodes.Status400BadRequest, ResponseDtoo.Err("there is no such file to remove"));

            // todo: check
            await fileStorageService.DeleteResourceFile(toRemove.PrivatePath);

            db.DocumentsResourceFiles.Remove(toRemove);
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDtoo.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDtoo.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ItemUpsertDto> response)> MarkDeleted(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var toDelete = await db.Documents.FindAsync(id);
            if (toDelete is null) return (StatusCodes.Status404NotFound, ResponseDto<ItemUpsertDto>.Err("cannot delete Document that does not exists"));
            if (!isUber && toDelete.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("cannot delete Document that does not belongs to this organization"));

            toDelete.Deleted = true;
            toDelete.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDto<ItemUpsertDto>.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ItemUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ItemUpsertDto> response)> MarkDeletedByConfigId(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var toDelete = db.Documents.Where(d => d.ConfigId == id);
            if (toDelete is null) return (StatusCodes.Status404NotFound, ResponseDto<ItemUpsertDto>.Err("cannot delete Documents that does not exists"));
            if (!isUber)
            {
                foreach (var doc in toDelete)
                {
                    if (doc.OrganizationId != currentOrgId)
                        return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("cannot delete Document that does not belongs to this organization"));
                }
            }
            foreach (var doc in toDelete)
            {
                doc.Deleted = true;
                doc.LastUpdated = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDto<ItemUpsertDto>.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ItemUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ItemUpsertDto> response)> MarkAsUndeleted(long currentOrgId, bool isUber, long id)
    {
        try
        {
            if (id == default) return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("the id cannot to be 0"));

            var toUndelete = await db.Documents.FindAsync(id);
            if (toUndelete is null) return (StatusCodes.Status404NotFound, ResponseDto<ItemUpsertDto>.Err("cannot undelete Document that does not exists"));
            if (!isUber && toUndelete.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("cannot undelete Document that does not belongs to this organization"));

            toUndelete.Deleted = false;
            toUndelete.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync();
            var ret = mapper.Map<ItemUpsertDto>(toUndelete);
            return (StatusCodes.Status200OK, ResponseDto<ItemUpsertDto>.Ok(ret));
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ItemUpsertDto>.Err(e.ToString()));
        }
    }
    public async Task<(int status, ResponseDto<ItemUpsertDto> response)> ReallyDelete(long currentOrgId, bool isUber, long id)
    {
        try
        {
            var toDelete = await db.Documents.FindAsync(id);
            if (toDelete is null) return (StatusCodes.Status404NotFound, ResponseDto<ItemUpsertDto>.Err("cannot delete Document that does not exists"));
            if (!isUber && toDelete.OrganizationId != currentOrgId) return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("cannot delete Document that does not belongs to this organization"));
            if (toDelete.Deleted == false) return (StatusCodes.Status400BadRequest, ResponseDto<ItemUpsertDto>.Err("cannot really delete Document that is not marked as deleted"));

            db.Documents.Remove(toDelete);
            await db.SaveChangesAsync();

            return (StatusCodes.Status200OK, ResponseDto<ItemUpsertDto>.Ok());
        }
        catch (Exception e)
        {
            return (StatusCodes.Status500InternalServerError, ResponseDto<ItemUpsertDto>.Err(e.ToString()));
        }
    }
}
