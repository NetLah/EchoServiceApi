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
        private readonly ILogger _logger;

        public CosmosCacheVerifier(IServiceProvider serviceProvider, ILogger<CosmosCacheVerifier> logger)
            : base(serviceProvider)
        {
            _logger = logger;
        }

        public async Task<VerifyResult> VerifyAsync(string name)
        {
            var connectionObj = GetConnection(name);
            var cosmosCacheInfo = connectionObj.Get<CosmosCacheInfo>();

            var databaseName = cosmosCacheInfo.DatabaseName;
            var containerName = cosmosCacheInfo.ContainerName;

            using var cosmosclient = await CreateClientAsync(connectionObj, cosmosCacheInfo);

            _logger.LogInformation("CosmosCacheVerifier db:{databaseName} container:{containerName} name={query_name}",
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
