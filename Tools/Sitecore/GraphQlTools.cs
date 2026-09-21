using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json.Nodes;

internal sealed class GraphQlTools
{
    [McpServerTool]
    [Description("Runs a raw GraphQL query against the configured Sitecore GraphQL schema and returns the response. Use this to query any Sitecore content or schema data via GraphQL.")]
    public async Task<string> QuerySitecoreGraphQl(
        [Description("The GraphQL query text, for example: query { item(path: \"/sitecore/content/Home\", language: \"en\") { id name path } }")] string query,
        [Description("GraphQL schema to target. Common values: edge, master, core. Defaults to the configured default schema.")] string? schema = null,
        [Description("Optional JSON object string containing GraphQL variables, for example: {\"path\": \"/sitecore/content/Home\"}")] string? variablesJson = null)
    {
        var settings = SitecoreSettings.LoadFromEnvironment();
        var client = new SitecoreGraphQlClient(settings);
        var resolvedSchema = string.IsNullOrWhiteSpace(schema) ? settings.GraphQl.GetDefaultSchema() : schema;
        JsonNode? variables = string.IsNullOrWhiteSpace(variablesJson) ? null : JsonNode.Parse(variablesJson);

        try
        {
            var response = await client.QueryAsync(query, resolvedSchema, variables);
            return ToolJson.Prettify(response);
        }
        catch (InvalidOperationException ex)
        {
            var resolvedEndpoint = settings.GraphQl.ResolveEndpoint(resolvedSchema);
            return $"GraphQL query failed for schema '{resolvedSchema}' at endpoint '{resolvedEndpoint}'. Error: {ex.Message}";
        }
    }
}
