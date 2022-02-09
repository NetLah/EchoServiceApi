using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers
{
    public class BlobUriVerifier : BaseVerifier
    {
        public BlobUriVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<VerifyResult> VerifyAsync(Uri blobUri)
        {
            var tokenCredential = TokenFactory.GetTokenCredential();
            var blobClient = new BlobClient(blobUri, tokenCredential);

            var isExist = (await blobClient.ExistsAsync()).Value;

            Response<BlobProperties>? response = !isExist ? null : await blobClient.GetPropertiesAsync();
            var props = response?.Value;

            var detail = $"created:{props?.CreatedOn}; modified:{props?.LastModified}";

            return new VerifySuccessMessage
            {
                Message = $"BlobUri '{blobUri}' is {(isExist ? "existed" : "not found")}",
                Detail = detail,
            };
        }

        public new ProviderConnectionString GetConnection(string name) => base.GetConnection(name);
    }
}
