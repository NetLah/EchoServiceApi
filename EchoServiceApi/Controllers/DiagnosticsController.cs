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
    }
}
