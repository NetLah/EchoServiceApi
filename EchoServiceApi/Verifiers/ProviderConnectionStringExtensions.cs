using NetLah.Extensions.Configuration;
using System.Data.Common;

namespace EchoServiceApi.Verifiers;

public class ConnectionCredentialValue : AzureCredentialInfo
{
    public string? Value { get; set; }
}

public class ConnectionCredentialBag<TValue> : AzureCredentialInfo
{
    public TValue? Value { get; set; }
}

public class ServiceBusFqns : AzureCredentialInfo
{
    /// <summary>
    /// fullyQualifiedNamespace
    /// </summary>
    public string? Fqns { get; set; }

    public string? QueueName { get; set; }
}

internal static class ProviderConnectionStringExtensions
{
    public static TConnectionCredentialInfo TryGet<TConnectionCredentialInfo>(this ProviderConnectionString connectionString)
        where TConnectionCredentialInfo : ConnectionCredentialValue, new()
    {
        try
        {
            _ = new DbConnectionStringBuilder { ConnectionString = connectionString.Value };
        }
        catch
        {
            return new TConnectionCredentialInfo { Value = connectionString.Value };
        }

        return connectionString.Get<TConnectionCredentialInfo>();
    }

    public static ConnectionCredentialBag<TValue> Load<TValue>(this ProviderConnectionString connectionString)
    {
        var result = connectionString.Get<ConnectionCredentialBag<TValue>>();
        result.Value = connectionString.Get<TValue>();
        return result;
    }

    public static ServiceBusFqns? GetServiceBusFqns(this ProviderConnectionString connectionString)
    {
        try
        {
            _ = new DbConnectionStringBuilder { ConnectionString = connectionString.Value };
        }
        catch
        {
            return new ServiceBusFqns { Fqns = connectionString.Value };
        }

        var result = connectionString.Get<ServiceBusFqns>();
        return result != null && !string.IsNullOrEmpty(result.Fqns) ? result : null;
    }
}
