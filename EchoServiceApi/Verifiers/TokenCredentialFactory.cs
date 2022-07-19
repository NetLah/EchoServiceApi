using Azure.Core;
using Azure.Identity;
using System.Collections.Concurrent;

namespace EchoServiceApi.Verifiers
{
    public class TokenCredentialFactory
    {
        private readonly IConfiguration _configuration;
        private readonly Lazy<TokenCredentialWrapper> _lazyDefault;
        private readonly ConcurrentDictionary<string, TokenCredentialWrapper> _managedIdentityClientIds;
        private readonly ConcurrentDictionary<ResourceIdentifier, TokenCredentialWrapper> _managedIdentityResourceIds;
        private readonly ConcurrentDictionary<string, TokenCredentialWrapper> _clientSecretsIdentities;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenCredentialFactory(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _lazyDefault = new Lazy<TokenCredentialWrapper>(() => new TokenCredentialWrapper(new DefaultAzureCredential(includeInteractiveCredentials: false), "default"));
            _managedIdentityClientIds = new ConcurrentDictionary<string, TokenCredentialWrapper>();
            _managedIdentityResourceIds = new ConcurrentDictionary<ResourceIdentifier, TokenCredentialWrapper>();
            _clientSecretsIdentities = new ConcurrentDictionary<string, TokenCredentialWrapper>();
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

        private async Task<TokenCredentialWrapper> PushCredentialTypeAsync(string credentialType, object? value, TokenCredentialWrapper tokenCredential)
        {
            async Task RenderCredentialAsync(HttpContext httpContext, Dictionary<string, object?> state)
            {
                await Task.CompletedTask;

                if (tokenCredential.CredentialInfo == null && tokenCredential.TokenCredential is { } tc)
                {
                }

                if (!string.IsNullOrEmpty(tokenCredential.CredentialInfo))
                {
                    state[$"credential_info"] = tokenCredential.CredentialInfo;
                }
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

        public async Task<TokenCredential> GetDefaultAzureCredentialAsync()
            => (await PushCredentialTypeAsync("default", null, _lazyDefault.Value)).TokenCredential;

        public async Task<TokenCredential> GetManagedIdentityClientIdAsync(string managedIdentityClientId)
            => (await PushCredentialTypeAsync("managedIdentityClientId", managedIdentityClientId,
                _managedIdentityClientIds.GetOrAdd(managedIdentityClientId,
                    _ => new TokenCredentialWrapper(new ManagedIdentityCredential(managedIdentityClientId), $"managedIdentityClientId={managedIdentityClientId}")))).TokenCredential;

        public Task<TokenCredential> GetManagedIdentityResourceIdAsync(string managedIdentityResourceId) =>
            GetManagedIdentityResourceIdAsync(new ResourceIdentifier(managedIdentityResourceId));

        public async Task<TokenCredential> GetManagedIdentityResourceIdAsync(ResourceIdentifier resourceId)
            => (await PushCredentialTypeAsync("managedIdentityResourceId", resourceId,
                _managedIdentityResourceIds.GetOrAdd(resourceId,
                    _ => new TokenCredentialWrapper(new ManagedIdentityCredential(resourceId), $"resourceId={resourceId}")))).TokenCredential;

        public async Task<TokenCredential> GetClientSecretCredentialAsync(string tenantId, string clientId, string clientSecret)
            => (await PushCredentialTypeAsync("clientId", clientId,
                _clientSecretsIdentities.GetOrAdd($"{tenantId}_{clientId}_{clientSecret}",
                    _ => new TokenCredentialWrapper(new ClientSecretCredential(tenantId, clientId, clientSecret), $"clientId={clientId}/tenantId={tenantId}")))).TokenCredential;
    }

    public class AzureCredentialInfo
    {
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? ManagedIdentityClientId { get; set; }
        public string? ManagedIdentityResourceId { get; set; }
    }

    public class TokenCredentialWrapper
    {
        public TokenCredentialWrapper(TokenCredential tokenCredential, string myInfo = null)
        {
            TokenCredential = tokenCredential;
            CredentialInfo = myInfo;
        }

        public TokenCredential TokenCredential { get; }

        public string? CredentialInfo { get; internal set; }
    }
}
