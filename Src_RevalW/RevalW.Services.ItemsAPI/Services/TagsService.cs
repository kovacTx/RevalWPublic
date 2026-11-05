using Microsoft.EntityFrameworkCore;
using RevalW.Services.ItemsAPI.Data;
using RevalW.Services.ItemsAPI.Model;
using RevalW.Shared.Model.Generic;

namespace RevalW.Services.ItemsAPI.Services;

public class TagsService(AppDbContext db)
{
    public async Task<(int status, ResponseDto<Tag> response)> CreateAndReturnEntity(long currentOrgId, bool isUber, Tag tag)
    {
        if (tag.Id != default) return (StatusCodes.Status400BadRequest, ResponseDto<Tag>.Err("the id have to be 0"));
        if (!isUber || tag.OrganizationId == default) tag.OrganizationId = currentOrgId;

        var lowerName = tag.Name.ToLower();
        if (db.Tags.Any(t => t.OrganizationId == currentOrgId && t.Name.ToLower() == lowerName)) return (StatusCodes.Status400BadRequest, ResponseDto<Tag>.Err("the Tag with this name already exists"));

        db.Tags.Add(tag);
        await db.SaveChangesAsync();

        return (StatusCodes.Status200OK, ResponseDto<Tag>.Ok(tag));
    }
    public Task<Tag?> GetEntityByName(long currentOrgId, bool isUber, string name)
    {
        var lowerName = name?.ToLower();
        return db.Tags.FirstOrDefaultAsync(i => i.Name.ToLower() == lowerName && (isUber || i.OrganizationId == currentOrgId));
    }
}
