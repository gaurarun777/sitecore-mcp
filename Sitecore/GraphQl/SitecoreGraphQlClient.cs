using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

internal sealed class SitecoreGraphQlClient
{
    private readonly SitecoreSettings _settings;

    public SitecoreGraphQlClient(SitecoreSettings settings)
    {
        _settings = settings;
    }

    public async Task<string> QueryAsync(string query, string? schema = null, object? variables = null)
    {
        var endpoint = _settings.GraphQl.ResolveEndpoint(schema);
        using var httpClient = SitecoreHttpClientFactory.Create();
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("sc_apikey", _settings.GraphQl.ApiKey);
        foreach (var header in _settings.GraphQl.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        request.Content = new StringContent(
            JsonSerializer.Serialize(new GraphQlRequest(query, variables)),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GraphQL request failed with status {(int)response.StatusCode}: {body}");
        }

        return body;
    }

    private sealed record GraphQlRequest(string Query, object? Variables);
}
