namespace EchoServiceApi.Verifiers;

public class HttpVerifier : BaseVerifier
{
    private readonly HttpClient _httpClient;

    public HttpVerifier(HttpClient httpClient, IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _httpClient = httpClient;
    }

    public async Task<string> VerifyAsync(Uri url, string? host)
    {
        if (url == null)
            throw new ArgumentException("Url is required");

        if (!string.IsNullOrEmpty(host))
        {
            _httpClient.DefaultRequestHeaders.Host = host;
        }

        Logger.LogInformation("HttpVerifier: url={query_url} host={query_host}", url, host);

        var content = await _httpClient.GetStringAsync(url);
        return content;
    }
}
