using NetLah.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace EchoServiceApi.Verifiers
{
    public class VerifyResult
    {
        public bool Success { get; set; } = true;

        public static VerifyResult Failed(Exception ex)
        {
            return new VerifyFailed
            {
                Success = false,
                Error = ex.Message,
                Detail = ex.ToString()
            };
        }

        public static VerifyResult Successed(string serviceName, ProviderConnectionString connectionObj)
        {
            return new VerifySuccessMessage
            {
                Message = $"{serviceName} '{connectionObj.Name}/{connectionObj.Provider}/{connectionObj.Custom}' is connected"
            };
        }
    }

    public class VerifyFailed : VerifyResult
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Error { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail { get; set; }
    }

    public class VerifySuccessMessage : VerifyResult
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }
    }
}
