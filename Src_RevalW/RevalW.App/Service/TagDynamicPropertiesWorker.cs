using Microsoft.AspNetCore.Mvc;
using RevalW.App.Mapper;
using RevalW.App.Model;
using RevalW.App.Model.IFace;
using RevalW.Shared.Model;
using RevalW.Shared.Utils;

namespace RevalW.App.Service;

public class TagDynamicPropertiesWorker(IServiceProvider serviceProvider, Controller controller)
{
    private IEnumerable<TagDto>? availableTags;

    private ConfigsService? _configsService;
    private ConfigsService ConfigsService => _configsService ??= serviceProvider.GetRequiredService<ConfigsService>();

    private ConfigsTagsService? _cTagsService;
    private ConfigsTagsService CTagsService => _cTagsService ??= serviceProvider.GetRequiredService<ConfigsTagsService>();

    public string? LastErrorMessage { get; set; }

    public async Task TryAddConfigsDynamicProperties(List<string>? tags)
    {
        if (tags is null) return;
        var tagsDto = await GetTagsDto(tags);
        if (tagsDto is null) return;

        var ret = new List<DynamicProperties4Tag>();
        foreach (var tag in tagsDto)
        {
            var dps = await GetDynamicProps4Config(tag.Id);
            if (dps is null) continue;
            ret.Add(new()
            {
                TagId = tag.Id,
                TagName = tag.Name,
                DynamicProperties = dps
            });
        }
        controller.ViewBag.ConfigsDynamicPropertiesDefinitions = ret;
    }
    public async Task<List<DynamicPropertyDto>?> GetDynamicProps4Config(long configId)
    {
        var newItemProperties4Config = await ConfigsService.GetDynamicProps4Config(configId);
        return newItemProperties4Config?.Result;
    }
    public async Task TrySetTagsJson(ITaggable? into)
    {
        if (into is null) return;
        var tagsDto = await GetTagsDto(into.Tags);
        if (tagsDto is null) return;
        into.TagsJson = MappingProfile.SerializeObject(tagsDto.ToArray());
    }
    public async Task<string?> TrySetAvailableTagsToViewBag()
    {
        var availableTags = await GetAvailableTags();
        if (availableTags is null) return LastErrorMessage ?? "";

        controller.ViewBag.AvailableTags = availableTags;
        return null;
    }
    public async Task<IEnumerable<TagDto>?> GetAvailableTags()
    {
        if (availableTags is not null) return availableTags;
        var tnResp = await CTagsService.GetAllAsync(false);
        if (!tnResp.IsOk())
        {
            LastErrorMessage = tnResp?.Message ?? "";
            return null;
        }
        availableTags = tnResp!.Result!;
        return availableTags;
    }

    private async Task<IEnumerable<TagDto>?> GetTagsDto(List<string>? tags)
    {
        if (tags is null) return null;
        return (await GetAvailableTags())?.Where(t => tags.Contains(t.Name));
    }
}
