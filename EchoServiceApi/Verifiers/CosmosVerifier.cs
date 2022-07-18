using Microsoft.Azure.Cosmos;
using NetLah.Extensions.Configuration;

#pragma warning disable S4457 // Parameter validation in "async"/"await" methods should be wrapped
namespace EchoServiceApi.Verifiers
{
    public class CosmosVerifier : BaseCosmosVerifier
    {
        public CosmosVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<VerifyResult> VerifyAsync(string name, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var connectionObj = GetConnection(name);
            var cosmosInfo = connectionObj.Get<CosmosContainerInfo>();

            var containerName = cosmosInfo.ContainerName;
            var databaseName = cosmosInfo.DatabaseName;

            using var cosmosclient = CreateClient(connectionObj, cosmosInfo);

            var container = cosmosclient.GetContainer(databaseName, containerName);
            _ = await container.ReadContainerAsync().ConfigureAwait(false);

            var itemResponse = await container.ReadItemAsync<Dictionary<string, object>>(key, new PartitionKey(key));
            return VerifyResult.SuccessObject("Cosmos", connectionObj, itemResponse.Resource);
        }
    }
}
#pragma warning restore S4457 // Parameter validation in "async"/"await" methods should be wrapped
