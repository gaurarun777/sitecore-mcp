using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

internal sealed class SitecorePowerShellClient
{
    private readonly PowerShellSettings _settings;

    public SitecorePowerShellClient(PowerShellSettings settings)
    {
        settings.EnsureConfigured();
        _settings = settings;
    }

    public async Task<string> ExecuteScriptAsync(string script)
    {
        var sessionId = Guid.NewGuid().ToString();
        using var httpClient = SitecoreHttpClientFactory.Create();
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildEndpointUrl(sessionId));
        request.Headers.Add("Authorization", BuildBasicAuthorizationHeader());
        request.Content = new StringContent($"{script}\r\n <#{sessionId}#>\r\n", Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"PowerShell remoting failed with status {(int)response.StatusCode}: {body}");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException(
                "PowerShell remoting returned HTTP 200 with an empty response body. " +
                "The SPE remoting endpoint is reachable, but it is not emitting script output.");
        }

        return NormalizeResponse(body);
    }

    private string BuildEndpointUrl(string sessionId)
    {
        return $"{_settings.ServerUrl.TrimEnd('/')}/-/script/script/?sessionId={sessionId}&rawOutput=false&persistentSession=False";
    }

    private string BuildBasicAuthorizationHeader()
    {
        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.Username}:{_settings.Password}"));
        return $"Basic {token}";
    }

    private static string NormalizeResponse(string response)
    {
        var trimmed = response.Trim();

        if (trimmed.StartsWith("#< CLIXML", StringComparison.Ordinal) ||
            trimmed.StartsWith("<Objs", StringComparison.Ordinal))
        {
            return trimmed;
        }

        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            trimmed = trimmed[1..^1]
                .Replace("\\r\\n", string.Empty, StringComparison.Ordinal)
                .Replace("\\n", string.Empty, StringComparison.Ordinal)
                .Replace("\\\"", "\"", StringComparison.Ordinal);
        }

        try
        {
            var parsed = JsonNode.Parse(trimmed);
            return parsed?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? trimmed;
        }
        catch
        {
            return trimmed;
        }
    }
}
