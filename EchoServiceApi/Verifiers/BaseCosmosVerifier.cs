using Microsoft.Azure.Cosmos;
using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers;

public class CosmosContainerInfo : AzureCredentialInfo
{
    public string? ContainerName { get; set; }
    public string? DatabaseName { get; set; }
    public string? AccountEndpoint { get; set; }
    public string? AccountKey { get; set; }
}

public abstract class BaseCosmosVerifier : BaseVerifier
{
    public BaseCosmosVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

    protected async Task<CosmosClient> CreateClientAsync(ProviderConnectionString providerConnectionString, CosmosContainerInfo cosmosInfo)
    {
        var cosmosClientOptions = providerConnectionString.Get<CosmosClientOptions>();
        var accountEndpoint = cosmosInfo.AccountEndpoint;

        var tokenCredential = await TokenFactory.GetTokenCredentialAsync(cosmosInfo);
        if (tokenCredential != null)
        {
            return new CosmosClient(accountEndpoint, tokenCredential, cosmosClientOptions);
        }

        var accountKey = cosmosInfo.AccountKey;
        if (!string.IsNullOrEmpty(accountKey))
        {
            await TokenFactory.PushCredentialTypeAsync("accountKey", TokenFactory.Redact(accountKey));
            return new CosmosClient(accountEndpoint, accountKey, cosmosClientOptions);
        }

        return new CosmosClient(accountEndpoint, await TokenFactory.GetTokenCredentialOrDefaultAsync(), cosmosClientOptions);
    }
}
