namespace Microsoft.Extensions.Configuration.Pkl;

internal class PklConfigurationParser
{
    private Dictionary<string, string?> Data = new();

    public IDictionary<string, string?> Parse(IDictionary<string, object?> source)
    {
        AddObject(source, "");
        return Data;
    }

    private void AddObject(IDictionary<string, object?> configuration, string objectKey)
    {
        foreach (var kvp in configuration)
        {
            var key = AppendToCurrentKey(objectKey, kvp.Key);
            AddValue(kvp.Value, key);
        }
    }

    private void AddValue(object? value, string key)
    {
        if (value is IDictionary<string, object?> objectMembers)
        {
            AddObject(objectMembers, key);
        }
        else if (value is IDictionary<object, object?> mapOrMapping)
        {
            AddMapping(mapOrMapping, key);
        }
        else if (value is IEnumerable<object> listOrListing)
        {
            AddListing(listOrListing, key);
        }
        else
        {
            Data[key] = value?.ToString();
        }
    }

    private void AddListing(IEnumerable<object> listOrListing, string listingKey)
    {
        var withIndex = listOrListing.Select((value, i) => (value, i));
        foreach (var (iValue, i) in withIndex)
        {
            var currentPositionKey = AppendToCurrentKey(listingKey, i.ToString());
            AddValue(iValue, currentPositionKey);
        }
    }

    private void AddMapping(IDictionary<object, object?> mapOrMapping, string mappingKey)
    {
        foreach (var kvp in mapOrMapping)
        {
            var key = kvp.Key?.ToString();

            if (key is null)
            {
                continue;
            }

            key = AppendToCurrentKey(mappingKey, key);
            AddValue(kvp.Value, key);
        }
    }

    private string AppendToCurrentKey(string currentKey, string path)
    {
        if (currentKey == "")
        {
            return path;
        }

        return $"{currentKey}:{path}";
    }
}
