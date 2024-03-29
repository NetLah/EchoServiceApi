﻿using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Verifiers;

public class BlobUriVerifier : BaseVerifier
{
    public BlobUriVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public async Task<VerifyResult> VerifyAsync(Uri blobUri)
    {
        var tokenCredential = await TokenFactory.GetTokenCredentialOrDefaultAsync();
        var blobClient = new BlobClient(blobUri, tokenCredential);

        using var scope = LoggerBeginScopeDiagnostic();

        Logger.LogInformation("BlobUriVerifier: Try access {query_blobUri}", blobUri);

        var isExist = (await blobClient.ExistsAsync()).Value;

        var response = !isExist ? null : await blobClient.GetPropertiesAsync();
        var props = response?.Value;

        var detail = $"created:{props?.CreatedOn}; modified:{props?.LastModified}";

        return new VerifySuccessMessage
        {
            Message = $"BlobUri '{blobUri}' is {(isExist ? "existed" : "not found")}",
            Detail = detail,
        };
    }

    public new ProviderConnectionString GetConnection(string name)
    {
        return base.GetConnection(name);
    }
}
