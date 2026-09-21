using System.Net.Http;

internal static class SitecoreHttpClientFactory
{
    public static HttpClient Create()
    {
        var allowInvalidCertificates = bool.TryParse(
            Environment.GetEnvironmentVariable("SITECORE_ALLOW_INVALID_CERTIFICATES"),
            out var parsedValue) && parsedValue;

        var handler = new HttpClientHandler();
        if (allowInvalidCertificates)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
    }
}
