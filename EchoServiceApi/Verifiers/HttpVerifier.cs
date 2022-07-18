namespace EchoServiceApi.Verifiers
{
    public class HttpVerifier : BaseVerifier
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public HttpVerifier(HttpClient httpClient, IServiceProvider serviceProvider, ILogger<HttpVerifier> logger)
            : base(serviceProvider)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> VerifyAsync(Uri url, string? host)
        {
            if (url == null)
                throw new ArgumentException("Url is required");

            if (!string.IsNullOrEmpty(host))
            {
                _httpClient.DefaultRequestHeaders.Host = host;
            }

            _logger.LogInformation("HttpVerifier: url={query_url} host={query_host}", url, host);

            var content = await _httpClient.GetStringAsync(url);
            return content;
        }
    }
}
