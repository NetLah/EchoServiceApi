﻿using Microsoft.Azure.Cosmos;
using NetLah.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace EchoServiceApi.Verifiers;

public class VerifyResult
{
    public bool Success { get; set; } = true;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; set; }

    public static VerifyResult Failed(Exception ex)
    {
        return ex is CosmosException cosmosException
            ? new VerifyFailed
            {
                Success = false,
                Error = $"{ex.GetType().FullName}: {cosmosException.Message}",
                Diagnostics = cosmosException.Diagnostics?.ToString(),
                StackTrace = cosmosException.StackTrace,
                DetailError = cosmosException.ToString(),
            }
            : (VerifyResult)new VerifyFailed
            {
                Success = false,
                Error = $"{ex.GetType().FullName}: {ex.Message}",
                StackTrace = ex.StackTrace,
                DetailError = ex.ToString(),
            };
    }

    public static VerifyResult Succeed(string serviceName, ProviderConnectionString connectionObj, string? detail = null)
    {
        return new VerifySuccessMessage
        {
            Message = $"{serviceName} '{connectionObj.Name}/{connectionObj.Provider}/{connectionObj.Custom}' is connected",
            Detail = detail,
        };
    }

    public static VerifyResult SuccessObject<TValue>(string serviceName, ProviderConnectionString connectionObj, TValue value, string? detail = null)
    {
        return new VerifySuccess<TValue>
        {
            Message = $"{serviceName} '{connectionObj.Name}/{connectionObj.Provider}/{connectionObj.Custom}' is connected",
            Value = value,
            Detail = detail,
        };
    }
}

public class VerifyFailed : VerifyResult
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Diagnostics { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StackTrace { get; internal set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DetailError { get; internal set; }
}

public class VerifySuccessMessage : VerifyResult
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }
}

public class VerifySuccess<TValue> : VerifySuccessMessage
{
    public TValue? Value { get; set; }
}
