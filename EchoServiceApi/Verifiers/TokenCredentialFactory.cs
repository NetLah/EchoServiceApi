using Azure.Core;
using Azure.Identity;
using System.Collections.Concurrent;

namespace EchoServiceApi.Verifiers
{
    public class TokenCredentialFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly Lazy<TokenCredential> _lazyDefault;
        private readonly ConcurrentDictionary<string, TokenCredential> _managedIdentityClientIds;
        private readonly ConcurrentDictionary<ResourceIdentifier, TokenCredential> _managedIdentityResourceIds;
        private readonly ConcurrentDictionary<string, TokenCredential> _clientSecretsIdentities;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenCredentialFactory(IConfiguration configuration, ILogger<TokenCredentialFactory> logger, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _logger = logger;
            _lazyDefault = new Lazy<TokenCredential>(() => new DefaultAzureCredential(includeInteractiveCredentials: false));
            _managedIdentityClientIds = new ConcurrentDictionary<string, TokenCredential>();
            _managedIdentityResourceIds = new ConcurrentDictionary<ResourceIdentifier, TokenCredential>();
            _clientSecretsIdentities = new ConcurrentDictionary<string, TokenCredential>();
            _httpContextAccessor = httpContextAccessor;
        }

        public TokenCredential GetTokenCredential()
        {
            var azure = _configuration.GetSection("Azure");

            var options = azure.Get<AzureCredentialInfo?>();
            if (options != null)
            {
                if (options.ManagedIdentityClientId is { } managedIdentityClientId)
                {
                    return GetManagedIdentityClientId(managedIdentityClientId);
                }

                if (options.ManagedIdentityResourceId is { } managedIdentityResourceId)
                {
                    return GetManagedIdentityResourceId(managedIdentityResourceId);
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

        public string? Redact(string? secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
                return null;

            secret = secret.Trim();
            if (secret.Length < 8)
            {
                return "REDACTED";
            }

            return $"{secret[..6]}-REDACTED";
        }

        public void PushCredentialType(string credentialType, object? value)
        {
            if (_httpContextAccessor.HttpContext is { } httpContext &&
                httpContext.Response is { } response)
            {
                var state = new Dictionary<string, object?> { ["credential_type"] = credentialType };

                if (credentialType != "default" || value != null)
                    state[$"credential_{credentialType}"] = value;

                var disposable = _logger.BeginScope(state);

                response.RegisterForDispose(disposable);
            }
        }

        public TokenCredential GetDefaultAzureCredential()
        {
            PushCredentialType("default", null);
            return _lazyDefault.Value;
        }

        public TokenCredential GetManagedIdentityClientId(string managedIdentityClientId)
        {
            PushCredentialType("managedIdentityClientId", managedIdentityClientId);
            return _managedIdentityClientIds.GetOrAdd(managedIdentityClientId, _ => new ManagedIdentityCredential(managedIdentityClientId));
        }

        public TokenCredential GetManagedIdentityResourceId(string managedIdentityResourceId) =>
            GetManagedIdentityResourceId(new ResourceIdentifier(managedIdentityResourceId));

        public TokenCredential GetManagedIdentityResourceId(ResourceIdentifier resourceId)
        {
            PushCredentialType("managedIdentityResourceId", resourceId);
            return _managedIdentityResourceIds.GetOrAdd(resourceId, _ => new ManagedIdentityCredential(resourceId));
        }

        public TokenCredential GetClientSecretCredential(string tenantId, string clientId, string clientSecret)
        {
            PushCredentialType("clientId", clientId);
            return _clientSecretsIdentities.GetOrAdd($"{tenantId}_{clientId}_{clientSecret}", _ => new ClientSecretCredential(tenantId, clientId, clientSecret));
        }
    }

    public class AzureCredentialInfo
    {
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? ManagedIdentityClientId { get; set; }
        public string? ManagedIdentityResourceId { get; set; }
    }
}
