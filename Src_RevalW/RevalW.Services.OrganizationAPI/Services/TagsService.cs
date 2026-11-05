using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RevalW.Services.OrganizationAPI.Data;
using RevalW.Services.OrganizationAPI.Model;
using RevalW.Shared.Model;
using RevalW.Shared.Model.Generic;
using System.Text.RegularExpressions;

namespace RevalW.Services.OrganizationAPI.Services;

public partial class TagsService(AppDbContext db, IMapper mapper)
{
    public (int status, ResponseDto<List<PublicOrganizationTagDto>> response) GetAllFullTags()
    {
        var tag = db.Tags.ToList();
        var ret = mapper.Map<List<PublicOrganizationTagDto>>(tag);
        return (StatusCodes.Status200OK, ResponseDto<List<PublicOrganizationTagDto>>.Ok(ret));
    }
    public (int status, ResponseDto<List<TagNameDto>> response) GetAllTags()
    {
        List<TagNameDto> ret;
        try
        {
            var tag = db.Tags.ToList();
            ret = mapper.Map<List<TagNameDto>>(tag);
        }
        catch
        {
            ret = [];
        }
        return (StatusCodes.Status200OK, ResponseDto<List<TagNameDto>>.Ok(ret));
    }
    public async Task<(int status, ResponseDto<PublicOrganizationTagDto> response)> GetTag(string name)
    {
        var tag = await db.Tags.Include(t => t.Organizations).FirstOrDefaultAsync(t => t.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        var ret = mapper.Map<PublicOrganizationTagDto>(tag);
        return (StatusCodes.Status200OK, ResponseDto<PublicOrganizationTagDto>.Ok(ret));
    }
    public (int status, ResponseDto<List<PublicOrganizationTagDto>> response) GetTags(IEnumerable<string> names)
    {
        var lowerNames = names.Select(n => n.ToLower());
        var tags = db.Tags.Include(t => t.Organizations).Where(t => lowerNames.Contains(t.Name.ToLower())).ToList();
        var ret = mapper.Map<List<PublicOrganizationTagDto>>(tags);
        return (StatusCodes.Status200OK, ResponseDto<List<PublicOrganizationTagDto>>.Ok(ret));
    }
    public List<Tag> GetTagsEntities(IEnumerable<string> names)
    {
        var tags = db.Tags.Where(t => names.Contains(t.Name));
        var ret = new List<Tag>();
        foreach (var t in tags)
        {
            db.Tags.Entry(t).State = EntityState.Detached;
            ret.Add(t);
        }
        return ret;
    }
    public void RemoveUnused()
    {
        var toRem = db.Tags.Include(t => t.Organizations).Where(t => !t.Organizations.Any()).ToList();
        db.Tags.RemoveRange(toRem);
    }
    public async Task UpdateTags(Organization org, IEnumerable<string> tags)
    {
        if (tags is null) return;
        org.Tags ??= [];
        await TryLoadTags(org);

        var existingTags = GetTagsEntities(tags);
        var existingNames = existingTags.Select(t => t.Name);
        var newTags = tags.Except(existingNames).ToList();
        foreach (var t in newTags)
        {
            if (string.IsNullOrWhiteSpace(t)) continue;
            var newTag = new Tag { Name = t.Replace(" ", "").ToLower() };
            db.Tags.Add(newTag);
            existingTags.Add(newTag);
        }

        // add to org
        var toAdd = existingTags.Where(et => !org.Tags.Any(t => t.Id == et.Id)).ToList();
        foreach (var tag in toAdd)
            org.Tags.Add(tag);
        // remove from org
        var toRemove = org.Tags.Where(t => !existingTags.Any(et => et.Id == t.Id)).ToList();
        foreach (var tag in toRemove)
            org.Tags.Remove(tag);

        await db.SaveChangesAsync();
    }

    private async Task TryLoadTags(Organization org)
    {
        if (org.Id == 0) return;

        var entry = db.Entry(org);
        if (!entry.Collection(o => o.Tags).IsLoaded)
            await entry.Collection(o => o.Tags).LoadAsync();
    }

    [GeneratedRegex(@"[\^\-\+\*\/\\\(\)\[\]\{\}\<\>\|\&\!\?\°\'\:;\,\.\s]")]
    private static partial Regex NameRegex();
}
