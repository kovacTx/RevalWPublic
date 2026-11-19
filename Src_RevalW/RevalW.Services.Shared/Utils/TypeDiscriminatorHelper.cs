using System.Text.Json;
using System.Text.Json.Nodes;

namespace RevalW.Services.Shared.Utils;

public static class TypeDiscriminatorHelper
{
    public static JsonArray? ReorderTypeDiscriminatorInArray(JsonDocument? doc)
    {
        if (doc == null) return null;

        var node = JsonNode.Parse(doc.RootElement.GetRawText());
        if (node is not JsonArray array) throw new InvalidOperationException("Top-level JSON is not an array.");

        var result = new JsonArray();
        foreach (var item in array)
        {
            if (item is not JsonObject obj)
            {
                result.Add(item);
                continue;
            }

            var reordered = new JsonObject();

            // Add $type first if present
            if (obj.TryGetPropertyValue("$type", out var typeNode)) MakeDuplicate(typeNode, reordered, "$type");

            // Add other properties, skipping $type
            foreach (var prop in obj)
                if (prop.Key != "$type") MakeDuplicate(prop.Value, reordered, prop.Key);

            result.Add(reordered);
        }
        return result;
    }
    private static void MakeDuplicate(JsonNode? node, JsonObject where, string key)
    {
        if (node is null)
        {
            where[key] = null;
            return;
        }

        var jsonNode = node?.ToJsonString() ?? "";
        where[key] = JsonNode.Parse(jsonNode);
    }
}
