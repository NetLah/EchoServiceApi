using Azure.Core;
using Azure.Identity;
using System.Collections.Concurrent;

namespace EchoServiceApi.Verifiers
{
    public class TokenCredentialFactory
    {
        private readonly IConfiguration _configuration;
        private readonly Lazy<TokenCredential> _lazyDefault;
        private readonly ConcurrentDictionary<string, TokenCredential> _managedIdentityClientIds;
        private readonly ConcurrentDictionary<ResourceIdentifier, TokenCredential> _managedIdentityResourceIds;
        private readonly ConcurrentDictionary<string, TokenCredential> _clientSecretsIdentities;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenCredentialFactory(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _lazyDefault = new Lazy<TokenCredential>(() => new DefaultAzureCredential(includeInteractiveCredentials: false));
            _managedIdentityClientIds = new ConcurrentDictionary<string, TokenCredential>();
            _managedIdentityResourceIds = new ConcurrentDictionary<ResourceIdentifier, TokenCredential>();
            _clientSecretsIdentities = new ConcurrentDictionary<string, TokenCredential>();
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<TokenCredential> GetTokenCredentialAsync()
        {
            var azure = _configuration.GetSection("Azure");

            var options = azure.Get<AzureCredentialInfo?>();
            if (options != null)
            {
                if (options.ManagedIdentityClientId is { } managedIdentityClientId)
                {
                    return GetManagedIdentityClientIdAsync(managedIdentityClientId);
                }

                if (options.ManagedIdentityResourceId is { } managedIdentityResourceId)
                {
                    return GetManagedIdentityResourceIdAsync(managedIdentityResourceId);
                }

                if (options.TenantId is { } tenantId &&
                    options.ClientId is { } clientId &&
                    options.ClientSecret is { } clientSecret)
                {
                    return GetClientSecretCredentialAsync(tenantId, clientId, clientSecret);
                }
            }

            return GetDefaultAzureCredentialAsync();
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

        private async Task<TokenCredential> PushCredentialTypeAsync(string credentialType, object? value, TokenCredential tokenCredential)
        {
            async Task RenderCredentialAsync(HttpContext httpContext, Dictionary<string, object?> state)
            {
                await Task.CompletedTask;
            }

            await PushCredentialTypeAsync(credentialType, value, RenderCredentialAsync);
            return tokenCredential;
        }

        public async Task PushCredentialTypeAsync(string credentialType, object? value, Func<HttpContext, Dictionary<string, object?>, Task>? func = null)
        {
            if (_httpContextAccessor.HttpContext is { } httpContext &&
                httpContext.Response is { } response)
            {
                var myInfos = httpContext.RequestServices.GetRequiredService<DisagnosticInfo>();
                var state = myInfos.LoggingScopeState;
                state["credential_type"] = credentialType;

                if (credentialType != "default" || value != null)
                    state[$"credential_{credentialType}"] = value;

                if (func != null)
                {
                    await func(httpContext, state);
                }
            }
        }

        public Task<TokenCredential> GetDefaultAzureCredentialAsync()
            => PushCredentialTypeAsync("default", null, _lazyDefault.Value);

        public Task<TokenCredential> GetManagedIdentityClientIdAsync(string managedIdentityClientId)
            => PushCredentialTypeAsync("managedIdentityClientId",
                managedIdentityClientId,
                _managedIdentityClientIds.GetOrAdd(managedIdentityClientId, _ => new ManagedIdentityCredential(managedIdentityClientId)));

        public Task<TokenCredential> GetManagedIdentityResourceIdAsync(string managedIdentityResourceId) =>
            GetManagedIdentityResourceIdAsync(new ResourceIdentifier(managedIdentityResourceId));

        public Task<TokenCredential> GetManagedIdentityResourceIdAsync(ResourceIdentifier resourceId)
            => PushCredentialTypeAsync("managedIdentityResourceId", resourceId, _managedIdentityResourceIds.GetOrAdd(resourceId, _ => new ManagedIdentityCredential(resourceId)));

        public Task<TokenCredential> GetClientSecretCredentialAsync(string tenantId, string clientId, string clientSecret)
            => PushCredentialTypeAsync("clientId", clientId,
                _clientSecretsIdentities.GetOrAdd($"{tenantId}_{clientId}_{clientSecret}", _ => new ClientSecretCredential(tenantId, clientId, clientSecret)));
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
