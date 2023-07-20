using Microsoft.Azure.Cosmos;
using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers;

public class CosmosVerifier : BaseCosmosVerifier
{
    public CosmosVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public async Task<VerifyResult> VerifyAsync(string name, string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        var connectionObj = GetConnection(name);
        var cosmosInfo = connectionObj.Get<CosmosContainerInfo>() ?? throw new Exception("CosmosContainerInfo is required");

        var containerName = cosmosInfo.ContainerName;
        var databaseName = cosmosInfo.DatabaseName;

        using var cosmosClient = (await CreateClientAsync(connectionObj, cosmosInfo)) ?? throw new Exception("CosmosContainerInfo is required");

        using var scope = LoggerBeginScopeDiagnostic();

        Logger.LogInformation("CosmosVerifier db:{databaseName} container:{containerName} name={query_name} key={query_key}",
            databaseName, containerName, name, key);

        var container = cosmosClient.GetContainer(databaseName, containerName);
        _ = await container.ReadContainerAsync().ConfigureAwait(false);

        var itemResponse = await container.ReadItemAsync<Dictionary<string, object>>(key, new PartitionKey(key));
        return VerifyResult.SuccessObject("Cosmos", connectionObj, itemResponse.Resource);
    }
}