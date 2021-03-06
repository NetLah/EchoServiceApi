using Microsoft.Extensions.Caching.Cosmos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers
{
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
            var cosmosCacheInfo = connectionObj.Get<CosmosCacheInfo>();

            var databaseName = cosmosCacheInfo.DatabaseName;
            var containerName = cosmosCacheInfo.ContainerName;

            using var cosmosclient = await CreateClientAsync(connectionObj, cosmosCacheInfo);

            using var scope = LoggerBeginScopeDiagnostic();

            Logger.LogInformation("CosmosCacheVerifier db:{databaseName} container:{containerName} name={query_name}",
                databaseName, containerName, name);

            var cosmosCacheOptions = new CosmosCacheOptions
            {
                CosmosClient = cosmosclient,
                ContainerName = containerName,
                DatabaseName = databaseName,
                CreateIfNotExists = false,
            };
            var cache = new CosmosCache(Options.Create(cosmosCacheOptions));
            _ = await cache.GetStringAsync("CosmosCacheVerifier");

            return VerifyResult.Successed("CosmosCache", connectionObj);
        }
    }
}
