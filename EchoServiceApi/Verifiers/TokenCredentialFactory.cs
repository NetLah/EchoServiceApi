﻿using Azure.Core;
using Azure.Identity;
using System.Collections.Concurrent;

namespace EchoServiceApi.Verifiers;

public class TokenCredentialFactory
{
    private readonly IConfiguration _configuration;
    private readonly Lazy<TokenCredentialWrapper> _lazyDefault;
    private readonly ConcurrentDictionary<string, TokenCredentialWrapper> _managedIdentities;
    private readonly ConcurrentDictionary<string, TokenCredentialWrapper> _clientSecretsIdentities;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenCredentialFactory(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions { ExcludeInteractiveBrowserCredential = true };
        configuration.Bind("DefaultAzureCredentialOptions", defaultAzureCredentialOptions);
        _lazyDefault = new Lazy<TokenCredentialWrapper>(() => new TokenCredentialWrapper(new DefaultAzureCredential(defaultAzureCredentialOptions), "default"));
        _managedIdentities = new ConcurrentDictionary<string, TokenCredentialWrapper>();
        _clientSecretsIdentities = new ConcurrentDictionary<string, TokenCredentialWrapper>();
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TokenCredential> GetTokenCredentialOrDefaultAsync(AzureCredentialInfo? options = null)
    {
        var result = options == null ? null : await GetTokenCredentialAsync(options);
        result ??= await GetDefaultTokenCredentialAsync();
        return result;
    }

    private async Task<TokenCredential> GetDefaultTokenCredentialAsync()
    {
        var azureCredentialConfig = _configuration.GetSection("Azure");
        var azureCredentialOptions = azureCredentialConfig.Get<AzureCredentialInfo?>();
        var result = await GetTokenCredentialAsync(azureCredentialOptions) ?? await GetDefaultAzureCredentialAsync();
        return result;
    }

    public async Task<TokenCredential?> GetTokenCredentialAsync(AzureCredentialInfo? options)
    {
        if (options != null)
        {
            if (options.CredentialType is { } credentialType && !string.IsNullOrWhiteSpace(credentialType))
            {
                return await GetByCredentialTypeAsync(credentialType, options);
            }
            else
            {
                if (options.ClientId is { } clientId)
                {
                    return options.TenantId is { } tenantId && options.ClientSecret is { } clientSecret
                        ? await GetClientSecretCredentialAsync(tenantId, clientId, clientSecret)
                        : await GetManagedIdentityClientIdAsync(clientId);
                }
            }
        }

        return null;
    }

    private Task<TokenCredential?> GetByCredentialTypeAsync(string credentialType, AzureCredentialInfo? options)
    {
        if ("AzureDeveloperCliCredential".Equals(credentialType, StringComparison.InvariantCultureIgnoreCase))
        {
            return Task.FromResult<TokenCredential?>(new AzureDeveloperCliCredential());
        }
        else if ("AzureCliCredential".Equals(credentialType, StringComparison.InvariantCultureIgnoreCase))
        {
            return Task.FromResult<TokenCredential?>(new AzureCliCredential());
        }

        return Task.FromResult<TokenCredential?>(null);
    }

    public string? Redact(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return null;
        }

        secret = secret.Trim();
        return secret.Length < 8 ? "REDACTED" : $"{secret[..6]}-REDACTED";
    }

    private async Task<TokenCredentialWrapper> PushCredentialTypeAsync(string credentialType, object? value, TokenCredentialWrapper tokenCredential)
    {
        async Task RenderCredentialAsync(HttpContext httpContext, Dictionary<string, object?> state)
        {
            await Task.CompletedTask;

            if (tokenCredential.CredentialInfo == null && tokenCredential.TokenCredential is { } tc)
            {
                // todo1: later find the way to get the credential information
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
        if (_httpContextAccessor.HttpContext is { } httpContext
            && httpContext.Response is { })
        {
            var state = httpContext.RequestServices.GetRequiredService<DiagnosticInfo>().LoggingScopeState;
            state["credential_type"] = credentialType;

            if (credentialType != "default" || value != null)
            {
                state[$"credential_{credentialType}"] = value;
            }

            if (func != null)
            {
                await func(httpContext, state);
            }
        }
    }

    private async Task<TokenCredential> GetDefaultAzureCredentialAsync()
    {
        return (await PushCredentialTypeAsync("default", null, _lazyDefault.Value)).TokenCredential;
    }

    private async Task<TokenCredential> GetManagedIdentityClientIdAsync(string clientId)
    {
        return (await PushCredentialTypeAsync("clientId", clientId,
                _managedIdentities.GetOrAdd(clientId,
                    _ => new TokenCredentialWrapper(new ManagedIdentityCredential(clientId), $"clientId={clientId}")))).TokenCredential;
    }

    private async Task<TokenCredential> GetClientSecretCredentialAsync(string tenantId, string clientId, string clientSecret)
    {
        return (await PushCredentialTypeAsync("clientSecret", clientId,
                _clientSecretsIdentities.GetOrAdd($"{tenantId}_{clientId}_{clientSecret}",
                    _ => new TokenCredentialWrapper(new ClientSecretCredential(tenantId, clientId, clientSecret), $"appId={clientId}/tenantId={tenantId}")))).TokenCredential;
    }
}

public class AzureCredentialInfo
{
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? CredentialType { get; set; }
}

public class TokenCredentialWrapper
{
    public TokenCredentialWrapper(TokenCredential tokenCredential, string? credentialInfo = null)
    {
        TokenCredential = tokenCredential;
        CredentialInfo = credentialInfo;
    }

    public TokenCredential TokenCredential { get; }

    public string? CredentialInfo { get; internal set; }
}
