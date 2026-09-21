using System.Text.Json;
using System.Text.Json.Nodes;

internal static class ToolJson
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(object? value)
    {
        return JsonSerializer.Serialize(value, IndentedOptions);
    }

    public static string Prettify(string json)
    {
        try
        {
            var parsed = JsonNode.Parse(json);
            return parsed?.ToJsonString(IndentedOptions) ?? json;
        }
        catch
        {
            return json;
        }
    }
}
