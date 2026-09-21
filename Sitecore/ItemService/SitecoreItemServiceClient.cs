using System.Text;
using System.Text.Json.Nodes;

internal sealed class SitecoreItemServiceClient
{
    private readonly ItemServiceSettings _settings;
    private string? _authCookie;

    public SitecoreItemServiceClient(ItemServiceSettings settings)
    {
        settings.EnsureConfigured();
        _settings = settings;
    }

    public async Task<JsonNode?> GetItemByPathAsync(string path, string? database = null, string? language = null)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["path"] = path
        };
        if (!string.IsNullOrWhiteSpace(database)) parameters["database"] = database;
        if (!string.IsNullOrWhiteSpace(language)) parameters["language"] = language;

        var requestUri = BuildUri("/sitecore/api/ssc/item", parameters);
        return await SendJsonAsync(HttpMethod.Get, requestUri);
    }

    private async Task EnsureAuthenticatedAsync(HttpClient httpClient)
    {
        if (!string.IsNullOrWhiteSpace(_authCookie))
        {
            return;
        }

        var loginUri = BuildUri("/sitecore/api/ssc/auth/login", null);
        using var request = new HttpRequestMessage(HttpMethod.Post, loginUri)
        {
            Content = new StringContent(
                ToolJson.Serialize(new
                {
                    username = _settings.Username,
                    password = _settings.Password,
                    domain = _settings.Domain
                }),
                Encoding.UTF8,
                "application/json")
        };

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            throw new InvalidOperationException("Item Service login succeeded but no authentication cookie was returned.");
        }

        var authCookie = cookies
            .SelectMany(static cookieHeader => cookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries))
            .Select(static cookiePart => cookiePart.Trim())
            .FirstOrDefault(static cookiePart => cookiePart.StartsWith(".AspNet.Cookies=", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(authCookie))
        {
            throw new InvalidOperationException("Item Service login did not return the .AspNet.Cookies authentication cookie.");
        }

        _authCookie = authCookie;
    }

    private async Task<JsonNode?> SendJsonAsync(HttpMethod method, string uri)
    {
        using var httpClient = SitecoreHttpClientFactory.Create();
        await EnsureAuthenticatedAsync(httpClient);

        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("Cookie", _authCookie);

        using var response = await httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Item Service request failed with status {(int)response.StatusCode}: {body}");
        }

        return string.IsNullOrWhiteSpace(body) ? null : JsonNode.Parse(body);
    }

    private string BuildUri(string relativePath, Dictionary<string, string>? parameters)
    {
        var baseUri = _settings.ServerUrl.TrimEnd('/');
        var builder = new System.Text.StringBuilder();
        builder.Append(baseUri);
        builder.Append(relativePath);

        if (parameters is { Count: > 0 })
        {
            builder.Append('?');
            builder.Append(string.Join('&', parameters.Select(static pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}")));
        }

        return builder.ToString();
    }
}
