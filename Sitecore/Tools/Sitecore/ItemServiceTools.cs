using ModelContextProtocol.Server;
using System.ComponentModel;

internal sealed class ItemServiceTools
{
    [McpServerTool]
    [Description("Retrieves a Sitecore item and its fields by its content tree path using the Sitecore Item Service REST API. Use this to read item data, field values, and metadata from any Sitecore item.")]
    public async Task<string> GetSitecoreItemByPath(
        [Description("Full Sitecore item path, for example: /sitecore/content/Home or /sitecore/content/Home/Articles/My-Article")] string path,
        [Description("Optional database name. Defaults to master. Common values: master, web, core.")] string? database = null,
        [Description("Optional language code. Defaults to en. For example: en, fr-FR, de-DE.")] string? language = null)
    {
        var client = new SitecoreItemServiceClient(SitecoreSettings.LoadFromEnvironment().ItemService);
        var node = await client.GetItemByPathAsync(path, database, language);
        return ToolJson.Serialize(node);
    }
}
