using Microsoft.Extensions.Caching.Cosmos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers;

public class CosmosCacheInfo : CosmosContainerInfo
{
    public bool CreateIfNotExists { get; set; }
}

public class CosmosCacheVerifier : BaseCosmosVerifier
{
    public CosmosCacheVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public async Task<VerifyResult> VerifyAsync(string name)
    {
        var connectionObj = GetConnection(name);
        var cosmosCacheInfo = connectionObj.Get<CosmosCacheInfo>() ?? throw new Exception("CosmosCacheInfo is required");

        var databaseName = cosmosCacheInfo.DatabaseName;
        var containerName = cosmosCacheInfo.ContainerName;

        using var cosmosClient = await CreateClientAsync(connectionObj, cosmosCacheInfo);

        using var scope = LoggerBeginScopeDiagnostic();

        Logger.LogInformation("CosmosCacheVerifier db:{databaseName} container:{containerName} name={query_name}",
            databaseName, containerName, name);

        var cosmosCacheOptions = new CosmosCacheOptions
        {
            CosmosClient = cosmosClient,
            ContainerName = containerName,
            DatabaseName = databaseName,
            CreateIfNotExists = false,
        };
        var cache = new CosmosCache(Options.Create(cosmosCacheOptions));
        _ = await cache.GetStringAsync("CosmosCacheVerifier");

        return VerifyResult.Succeed("CosmosCache", connectionObj);
    }
}
