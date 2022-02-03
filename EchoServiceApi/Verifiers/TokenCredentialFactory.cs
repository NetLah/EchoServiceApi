using Azure.Core;
using Azure.Identity;

namespace EchoServiceApi.Verifiers
{
    public class TokenCredentialFactory
    {
        private readonly Lazy<TokenCredential> _lazy;

        public TokenCredentialFactory(IConfiguration configuration)
        {
            _lazy = new Lazy<TokenCredential>(() => GetTokenCredential(configuration));
        }

        private TokenCredential GetTokenCredential(IConfiguration configuration)
        {
            var azure = configuration.GetSection("Azure");

            var options = azure.Get<AzureCredentialInfo>();
            if (options.TenantId is { } tenantId &&
                options.ClientId is { } clientId &&
                options.ClientSecret is { } clientSecret)
            {
                return new ClientSecretCredential(tenantId, clientId, clientSecret);
            }

            return new DefaultAzureCredential(includeInteractiveCredentials: false);
        }

        public TokenCredential GetTokenCredential() => _lazy.Value;
    }

    public class AzureCredentialInfo
    {
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
    }
}
