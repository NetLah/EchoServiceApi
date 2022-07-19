using Microsoft.Azure.Cosmos;
using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers;

public class CosmosContainerInfo
{
    public string? ContainerName { get; set; }
    public string? DatabaseName { get; set; }
    public string? AccountEndpoint { get; set; }
    public string? AccountKey { get; set; }
    public string? ManagedIdentityClientId { get; set; }
    public string? ManagedIdentityResourceId { get; set; }
}

public abstract class BaseCosmosVerifier : BaseVerifier
{
    public BaseCosmosVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

    protected async Task<CosmosClient> CreateClientAsync(ProviderConnectionString providerConnectionString, CosmosContainerInfo cosmosInfo)
    {
        var cosmosClientOptions = providerConnectionString.Get<CosmosClientOptions>();
        var accountEndpoint = cosmosInfo.AccountEndpoint;
        var managedIdentityClientId = cosmosInfo.ManagedIdentityClientId;

        if (!string.IsNullOrEmpty(managedIdentityClientId))
            return new CosmosClient(accountEndpoint, await TokenFactory.GetManagedIdentityClientIdAsync(managedIdentityClientId), cosmosClientOptions);

        var managedIdentityResourceId = cosmosInfo.ManagedIdentityResourceId;
        if (!string.IsNullOrEmpty(managedIdentityResourceId))
            return new CosmosClient(accountEndpoint, await TokenFactory.GetManagedIdentityResourceIdAsync(managedIdentityResourceId), cosmosClientOptions);

        var accountKey = cosmosInfo.AccountKey;
        if (!string.IsNullOrEmpty(accountKey))
        {
            await TokenFactory.PushCredentialTypeAsync("accountKey", TokenFactory.Redact(accountKey));
            return new CosmosClient(accountEndpoint, accountKey, cosmosClientOptions);
        }

        return new CosmosClient(accountEndpoint, await TokenFactory.GetTokenCredentialAsync(), cosmosClientOptions);
    }
}
