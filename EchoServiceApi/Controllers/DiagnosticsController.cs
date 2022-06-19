using EchoServiceApi.Verifiers;
using Microsoft.AspNetCore.Mvc;

namespace EchoServiceApi.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        public IActionResult Connection([FromServices] HttpContextInfo httpContextInfo)
        {
            try
            {
                var request = HttpContext.Request;
                var remote = HttpContext.Connection;
                var remoteIpAddress = remote.RemoteIpAddress?.ToString();
                if (remoteIpAddress?.Contains(':') == true)
                {
                    remoteIpAddress = $"[{remoteIpAddress}]";
                }
                var connectionInfo = $"Server:{request.Scheme}://{httpContextInfo.Host}:{httpContextInfo.Port} Client:{remoteIpAddress}:{remote?.RemotePort}";
                return Ok(connectionInfo);
            }
            catch (Exception ex)
            {
                return Ok(VerifyResult.Failed(ex));
            }
        }

        public async Task<IActionResult> CosmosCacheAsync([FromServices] CosmosCacheVerifier cosmosVerifier, string name)
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

        public async Task<IActionResult> CosmosAsync([FromServices] CosmosVerifier cosmosVerifier, string name, string key)
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

        public async Task<IActionResult> PostgreSqlAsync([FromServices] PosgreSqlVerifier posgreSqlVerifier, string name, string tableName)
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

        public async Task<IActionResult> KeyVaultCertificateAsync([FromServices] KeyVaultCertificateVerifier keyVaultCertificateVerifier, string name, bool privateKey)
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

        public async Task<IActionResult> KeyVaultKeyAsync([FromServices] KeyVaultKeyVerifier keyVaultKeyVerifier, string name)
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

        public async Task<IActionResult> BlobUriAsync([FromServices] BlobUriVerifier blobUriVerifier, string nameOrUrl)
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

        public async Task<IActionResult> DirAsync([FromServices] DirVerifier dirVerifier, string path)
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

        public async Task<IActionResult> MessageBusAsync([FromServices] MessageBusVerifier messageBusVerifier, string name, bool send, bool receive, string? queueName)
        {
            try
            {
                var result = await messageBusVerifier.VerifyAsync(name, send, receive, queueName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(VerifyResult.Failed(ex));
            }
        }

        public async Task<IActionResult> HttpAsync([FromServices] HttpVerifier httpVerifier, Uri url, string? host)
        {
            try
            {
                var result = await httpVerifier.VerifyAsync(url, host);
                return Content(result);
            }
            catch (Exception ex)
            {
                return Ok(VerifyResult.Failed(ex));
            }
        }
    }
}
