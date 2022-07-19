using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers
{
    public class BlobUriVerifier : BaseVerifier
    {
        private readonly ILogger _logger;

        public BlobUriVerifier(IServiceProvider serviceProvider, ILogger<BlobUriVerifier> logger)
            : base(serviceProvider)
        {
            _logger = logger;
        }

        public async Task<VerifyResult> VerifyAsync(Uri blobUri)
        {
            var tokenCredential = await TokenFactory.GetTokenCredentialAsync();
            var blobClient = new BlobClient(blobUri, tokenCredential);

            _logger.LogInformation("BlobUriVerifier: Try access {query_blobUri}", blobUri);

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
