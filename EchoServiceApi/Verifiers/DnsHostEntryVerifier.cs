using System.Net;

namespace EchoServiceApi.Verifiers
{
    public class DnsHostEntryVerifier : BaseVerifier
    {
        public DnsHostEntryVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public Task<VerifyResult> VerifyAsync(string? host)
        {
            host = string.IsNullOrWhiteSpace(host) ? string.Empty : host.Trim();

            Logger.LogInformation("DnsHostEntryVerifier: host={host}", host);

            var ipHostEntry = Dns.GetHostEntry(host);
            var addressList = ipHostEntry.AddressList?.Select(ip => ip.ToString()).ToArray();
            var result = new
            {
                AddressList = addressList,
                ipHostEntry.HostName,
                ipHostEntry.Aliases,
            };

            return Task.FromResult<VerifyResult>(new VerifySuccess<object>
            {
                Message = $"DNS Lookup {host}",
                Value = result
            });
        }
    }
}
