using System.Text.Json;

internal sealed record SitecoreSettings(
    GraphQlSettings GraphQl,
    ItemServiceSettings ItemService,
    PowerShellSettings PowerShell)
{
    public static SitecoreSettings LoadFromEnvironment()
    {
        return new SitecoreSettings(
            new GraphQlSettings(
                GetRequired("GRAPHQL_ENDPOINT"),
                (Environment.GetEnvironmentVariable("GRAPHQL_SCHEMAS") ?? "edge")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                GetRequired("GRAPHQL_API_KEY"),
                ParseHeaders(Environment.GetEnvironmentVariable("GRAPHQL_HEADERS"))),
            new ItemServiceSettings(
                Environment.GetEnvironmentVariable("ITEM_SERVICE_SERVER_URL") ?? string.Empty,
                Environment.GetEnvironmentVariable("ITEM_SERVICE_USERNAME") ?? string.Empty,
                Environment.GetEnvironmentVariable("ITEM_SERVICE_PASSWORD") ?? string.Empty,
                Environment.GetEnvironmentVariable("ITEM_SERVICE_DOMAIN") ?? "sitecore"),
            new PowerShellSettings(
                Environment.GetEnvironmentVariable("POWERSHELL_SERVER_URL") ?? string.Empty,
                Environment.GetEnvironmentVariable("POWERSHELL_USERNAME") ?? string.Empty,
                Environment.GetEnvironmentVariable("POWERSHELL_PASSWORD") ?? string.Empty,
                Environment.GetEnvironmentVariable("POWERSHELL_DOMAIN") ?? "sitecore"));
    }

    private static string GetRequired(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"The environment variable '{name}' is required.");
        }

        return value;
    }

    private static IReadOnlyDictionary<string, string> ParseHeaders(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ??
               new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}

internal sealed record GraphQlSettings(
    string Endpoint,
    IReadOnlyList<string> Schemas,
    string ApiKey,
    IReadOnlyDictionary<string, string> Headers)
{
    public string ResolveEndpoint(string? schema = null)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return Endpoint;
        }

        var trimmedEndpoint = Endpoint.TrimEnd('/');
        foreach (var configuredSchema in Schemas)
        {
            if (trimmedEndpoint.EndsWith($"/{configuredSchema}", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedEndpoint[..^(configuredSchema.Length)] + schema;
            }
        }

        return $"{trimmedEndpoint}/{schema}";
    }

    public string GetDefaultSchema() => Schemas.FirstOrDefault() ?? "edge";
}

internal sealed record ItemServiceSettings(string ServerUrl, string Username, string Password, string Domain)
{
    public void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl) ||
            string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException("Item Service settings are not fully configured in environment variables.");
        }
    }
}

internal sealed record PowerShellSettings(string ServerUrl, string Username, string Password, string Domain)
{
    public void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl) ||
            string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException("PowerShell settings are not fully configured in environment variables.");
        }
    }
}
