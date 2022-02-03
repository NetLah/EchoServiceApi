using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Cosmos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers
{
    public class CosmosCacheVerifier : BaseVerifier
    {
        public CosmosCacheVerifier(IConfiguration configuration) : base(configuration) { }

        public override async Task<VerifyResult> VerifyAsync(string name)
        {
            var connectionObj = GetConnection(name);
            var cosmosCacheInfo = connectionObj.Get<CosmosCacheInfo>();
            var cosmosClientOptions = connectionObj.Get<CosmosClientOptions>();

            var containerName = cosmosCacheInfo.ContainerName;
            var databaseName = cosmosCacheInfo.DatabaseName;
            var accountEndpoint = cosmosCacheInfo.AccountEndpoint;
            var accountKey = cosmosCacheInfo.AccountKey;

            var cosmosclient = !string.IsNullOrEmpty(accountKey) ?
                new CosmosClient(accountEndpoint, accountKey, cosmosClientOptions) :
                new CosmosClient(accountEndpoint, new DefaultAzureCredential(includeInteractiveCredentials: false), cosmosClientOptions);

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

    public class CosmosCacheInfo
    {
        public bool CreateIfNotExists { get; set; }
        public string? ContainerName { get; set; }
        public string? DatabaseName { get; set; }
        public string? AccountEndpoint { get; set; }
        public string? AccountKey { get; set; }
    }
}
