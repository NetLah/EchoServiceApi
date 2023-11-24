using Microsoft.AspNetCore.Mvc;
using NetLah.Extensions.Configuration;

namespace EchoServiceApi.Controllers;

[Route("[controller]/[action]")]
public class DumpController : ControllerBase
{
    private static IDictionary<string, string?> GetEnvironmentVariables()
    {
        var result = new Dictionary<string, string?>();
        foreach (System.Collections.DictionaryEntry item in Environment.GetEnvironmentVariables())
        {
            result.Add((string)item.Key, (string?)item.Value);
        }
        return result;
    }

    public JsonResult AppSettings([FromServices] IConfiguration configuration, string? env)
    {
        var environmentVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(env) || !new[] { "true", "t", "yes", "y", "1" }.Contains(env.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            environmentVariables = GetEnvironmentVariables()
                .Select(kv => $"{kv.Key}-{kv.Value}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return new(
            configuration
                .AsEnumerable(true)
                .Where(kv => kv.Value != null && !environmentVariables.Contains($"{kv.Key}-{kv.Value}"))
                .OrderBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value));
    }

    public JsonResult ConnectionStrings([FromServices] IConfiguration configuration)
    {
        return new(
            new ConnectionStringManager(configuration)
                .ConnectionStrings
                .ToDictionary(c => c.Key, c => new
                {
                    c.Value.Raw,
                    Provider = c.Value.Provider.ToString(),
                    c.Value.Custom,
                }));
    }

    public JsonResult Environments()
    {
        return new(
            GetEnvironmentVariables()
                .OrderBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value));
    }
}
