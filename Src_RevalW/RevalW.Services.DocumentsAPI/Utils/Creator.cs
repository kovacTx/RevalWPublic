using RevalM.SharedStorage.LSData;

namespace RevalW.Services.DocumentsAPI.Utils;

public class Creator
{
    const char OpenIndexBracket = '[';
    const char CloseIndexBracket = ']';
    const char IndexValueSparator = ':';
    const char InnerVariablePrefix = '$';

    public DataHolder Create(List<ValueWrapper>? values)
    {
        var ret = new DataHolder();
        if (values is null) return ret;
        foreach (var value in values)
        {
            if (value?.Name is null) continue;
            var myHolder = FindOrCreateName(value.Name, ret);
            if (myHolder is null) continue;
            myHolder.Item = value;
        }
        ReindexInner(ret);
        return ret;
    }

    private static DataHolder? FindOrCreateName(string name, DataHolder mainHolder)
    {
        var indexes = Explode(name);
        if (indexes is null) return null;
        var myHolder = mainHolder;
        foreach (var part in indexes.Parts)
        {
            var innerName = part.ItemName;
            var innerIndex = part.ItemIndex;
            var innerIndexName = part.IndexName;
            myHolder = GetHolder(myHolder, innerName.Replace(InnerVariablePrefix.ToString(), ""), null);
            if (!string.IsNullOrEmpty(innerIndexName) && innerIndex.HasValue) myHolder = GetHolder(myHolder, innerIndexName.Replace(InnerVariablePrefix.ToString(), ""), innerIndex);
        }
        return myHolder;
    }
    private static DataHolder GetHolder(DataHolder holder, string innerName, int? innerIndex)
    {
        holder.Inner ??= [];
        var ret = holder.Inner.Where(inner => inner.Name == innerName && inner.Index == innerIndex).FirstOrDefault();
        if (ret is not null) return ret;
        ret = new DataHolder
        {
            Index = innerIndex,
            Name = innerName,
        };
        holder.Inner.Add(ret);
        return ret;
    }
    private static void ReindexInner(DataHolder? holder)
    {
        if (holder is null) return;
        if (holder.Inner is null) return;
        if (holder.Inner.Count < 2) return;
        var withIndex = holder.Inner.Where(inner => inner.Index.HasValue).ToList();
        var withoutIndex = holder.Inner.Where(inner => !inner.Index.HasValue);
        withIndex.Sort((in1, in2) =>
        {
            if (!in1.Index.HasValue) return 0;
            if (!in2.Index.HasValue) return 0;
            return in1.Index.Value.CompareTo(in2.Index.Value);
        });
        holder.Inner = withIndex;
        holder.Inner.AddRange(withoutIndex);
        foreach (var inner in holder.Inner)
        {
            ReindexInner(inner);
        }
    }
    private static DataIndexes? Explode(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var parts = name.Split('.');
        var ret = new DataIndexes();
        foreach (var part in parts)
        {
            var itemName = GetNameWithoutOpenIndexBracket(part);
            var itemIndex = GetIndexValue(part);
            var indexName = !itemIndex.HasValue ? null : GetIndexReference(part);
            ret.Parts.AddLast(new DataIndexes.PathPart()
            {
                ItemName = itemName,
                ItemIndex = itemIndex,
                IndexName = indexName
            });
        }
        return ret;
    }
    private static string? GetIndexReference(string part)
    {
        var bracketStartIndex = part.IndexOf(OpenIndexBracket);
        if (bracketStartIndex < 0) return null;
        var colonIndex = part.LastIndexOf(IndexValueSparator);
        if (colonIndex < 0) return null;
        var bracketEndIndex = part.IndexOf(CloseIndexBracket);
        if (bracketEndIndex < 0) return null;
        bracketStartIndex++;
        return part[bracketStartIndex..colonIndex];
    }
    private static int? GetIndexValue(string part)
    {
        var directIndex = GetIndexReference(part);
        if (int.TryParse(directIndex, out int ret)) return ret;
        var colonIndex = part.LastIndexOf(IndexValueSparator);
        if (colonIndex < 0) return null;
        colonIndex++;
        var bracketIndex = part.IndexOf(CloseIndexBracket);
        if (bracketIndex < 0) return null;
        var indexStrValue = part[colonIndex..bracketIndex];
        if (int.TryParse(indexStrValue, out ret)) return ret;
        return -1;
    }
    public static string GetNameWithoutOpenIndexBracket(string part)
    {
        var startOfIndex = part.IndexOf(OpenIndexBracket);
        if (startOfIndex < 0) return part;
        return part[..startOfIndex];
    }

    public class DataHolder
    {
        public string? Name { get; set; }
        public int? Index { get; set; }
        public ValueWrapper Item { get; set; } = null!;
        public List<DataHolder> Inner { get; set; } = null!;

        public override string? ToString()
        {
            string? ret;
            if (!Index.HasValue) ret = Name;
            else ret = Name + "[" + Index + "]";
            if (Inner is not null) ret += "(" + Inner.Count + ")";
            return ret;
        }
    }
}
