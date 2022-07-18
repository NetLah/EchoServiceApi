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
}

public abstract class BaseCosmosVerifier : BaseVerifier
{
    public BaseCosmosVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

    protected CosmosClient CreateClient(ProviderConnectionString providerConnectionString, CosmosContainerInfo cosmosInfo)
    {
        var cosmosClientOptions = providerConnectionString.Get<CosmosClientOptions>();
        var accountEndpoint = cosmosInfo.AccountEndpoint;
        var accountKey = cosmosInfo.AccountKey;
        var managedIdentityClientId = cosmosInfo.ManagedIdentityClientId;

        if (!string.IsNullOrEmpty(managedIdentityClientId))
            return new CosmosClient(accountEndpoint, TokenFactory.GetManagedIdentity(managedIdentityClientId), cosmosClientOptions);

        if (!string.IsNullOrEmpty(accountKey))
            return new CosmosClient(accountEndpoint, accountKey, cosmosClientOptions);

        return new CosmosClient(accountEndpoint, TokenFactory.GetTokenCredential(), cosmosClientOptions);
    }
}
