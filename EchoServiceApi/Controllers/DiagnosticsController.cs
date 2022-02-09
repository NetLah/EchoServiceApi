using EchoServiceApi.Verifiers;
using Microsoft.AspNetCore.Mvc;

namespace EchoServiceApi.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        public async Task<IActionResult> CosmosCache([FromServices] CosmosCacheVerifier cosmosVerifier, string name)
        {
            try
            {
                var result = await cosmosVerifier.VerifyAsync(name);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(VerifyResult.Failed(ex));
            }
        }

        public async Task<IActionResult> Cosmos([FromServices] CosmosVerifier cosmosVerifier, string name, string key)
        {
            try
            {
                var result = await cosmosVerifier.VerifyAsync(name, key);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(VerifyResult.Failed(ex));
            }
        }

        public async Task<IActionResult> PostgreSql([FromServices] PosgreSqlVerifier posgreSqlVerifier, string name, string tableName)
        {
            try
            {
                var result = await posgreSqlVerifier.VerifyAsync(name, tableName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(VerifyResult.Failed(ex));
            }
        }

        public async Task<IActionResult> KeyVaultCertificate([FromServices] KeyVaultCertificateVerifier keyVaultCertificateVerifier, string name, bool privateKey)
        {
            try
            {
                var result = await keyVaultCertificateVerifier.VerifyAsync(name, privateKey);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(VerifyResult.Failed(ex));
            }
        }

        public async Task<IActionResult> KeyVaultKey([FromServices] KeyVaultKeyVerifier keyVaultKeyVerifier, string name)
        {
            try
            {
                var result = await keyVaultKeyVerifier.VerifyAsync(name);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(VerifyResult.Failed(ex));
            }
        }

        public async Task<IActionResult> BlobUri([FromServices] BlobUriVerifier blobUriVerifier, string nameOrUrl)
        {
            try
            {
                if (Uri.TryCreate(nameOrUrl, UriKind.Absolute, out var blobUri))
                {
                    var result = await blobUriVerifier.VerifyAsync(blobUri);
                    return Ok(result);
                }
                else
                {
                    var connectionObj = blobUriVerifier.GetConnection(nameOrUrl);
                    var blobUri1 = new Uri(connectionObj.Value);
                    var result = await blobUriVerifier.VerifyAsync(blobUri1);
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return Ok(VerifyResult.Failed(ex));
            }
        }

        public async Task<IActionResult> Dir([FromServices] DirVerifier dirVerifier, string path)
        {
            try
            {
                var result = await dirVerifier.VerifyAsync(path);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(VerifyResult.Failed(ex));
            }
        }
    }
}
