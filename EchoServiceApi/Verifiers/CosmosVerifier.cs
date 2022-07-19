using Microsoft.Azure.Cosmos;
using NetLah.Extensions.Configuration;

#pragma warning disable S4457 // Parameter validation in "async"/"await" methods should be wrapped
namespace EchoServiceApi.Verifiers
{
    public class CosmosVerifier : BaseCosmosVerifier
    {
        private readonly ILogger _logger;

        public CosmosVerifier(IServiceProvider serviceProvider, ILogger<CosmosVerifier> logger) : base(serviceProvider)
        {
            _logger = logger;
        }

        public async Task<VerifyResult> VerifyAsync(string name, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var connectionObj = GetConnection(name);
            var cosmosInfo = connectionObj.Get<CosmosContainerInfo>();

            var containerName = cosmosInfo.ContainerName;
            var databaseName = cosmosInfo.DatabaseName;

            using var cosmosclient = await CreateClientAsync(connectionObj, cosmosInfo);

            _logger.LogInformation("CosmosVerifier db:{databaseName} container:{containerName} name={query_name} key={query_key}",
                databaseName, containerName, name, key);

            var container = cosmosclient.GetContainer(databaseName, containerName);
            _ = await container.ReadContainerAsync().ConfigureAwait(false);

            var itemResponse = await container.ReadItemAsync<Dictionary<string, object>>(key, new PartitionKey(key));
            return VerifyResult.SuccessObject("Cosmos", connectionObj, itemResponse.Resource);
        }
    }
}
#pragma warning restore S4457 // Parameter validation in "async"/"await" methods should be wrapped
