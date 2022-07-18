using Azure.Core;
using Azure.Identity;
using System.Collections.Concurrent;

namespace EchoServiceApi.Verifiers
{
    public class TokenCredentialFactory
    {
        private readonly Lazy<TokenCredential> _lazy;
        private readonly Lazy<TokenCredential> _lazyDefault;
        private readonly ConcurrentDictionary<string, TokenCredential> _managedIdentities;
        private readonly ConcurrentDictionary<string, TokenCredential> _clientSecretsIdentities;

        public TokenCredentialFactory(IConfiguration configuration)
        {
            _lazy = new Lazy<TokenCredential>(() => GetTokenCredential(configuration));
            _lazyDefault = new Lazy<TokenCredential>(() => new DefaultAzureCredential(includeInteractiveCredentials: false));
            _managedIdentities = new ConcurrentDictionary<string, TokenCredential>();
            _clientSecretsIdentities = new ConcurrentDictionary<string, TokenCredential>();
        }

        private TokenCredential GetTokenCredential(IConfiguration configuration)
        {
            var azure = configuration.GetSection("Azure");

            var options = azure.Get<AzureCredentialInfo?>();
            if (options != null)
            {
                if (options.ManagedIdentityClientId is { } managedIdentityClientId)
                {
                    return GetManagedIdentity(managedIdentityClientId);
                }

                if (options.TenantId is { } tenantId &&
                    options.ClientId is { } clientId &&
                    options.ClientSecret is { } clientSecret)
                {
                    return GetClientSecretCredential(tenantId, clientId, clientSecret);
                }
            }

            return GetDefaultAzureCredential();
        }

        public TokenCredential GetTokenCredential() => _lazy.Value;

        public TokenCredential GetDefaultAzureCredential() => _lazyDefault.Value;

        public TokenCredential GetManagedIdentity(string managedIdentityClientId) =>
            _managedIdentities.GetOrAdd(managedIdentityClientId, _ => new ManagedIdentityCredential(managedIdentityClientId));

        public TokenCredential GetClientSecretCredential(string tenantId, string clientId, string clientSecret) =>
            _clientSecretsIdentities.GetOrAdd($"{tenantId}_{clientId}_{clientSecret}", _ => new ClientSecretCredential(tenantId, clientId, clientSecret));
    }

    public class AzureCredentialInfo
    {
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? ManagedIdentityClientId { get; set; }
    }
}
