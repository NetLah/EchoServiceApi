using NetLah.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace EchoServiceApi.Verifiers;

public class CertificateVerifier : BaseVerifier
{
    public CertificateVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

    private static string FormatCert(X509Certificate2 cert, bool privateKey)
    {
        return $"Subject={cert.Subject}; Expires={cert.GetExpirationDateString()}; HasPrivateKey={privateKey}";
    }

    public Task<VerifyResult> VerifyAsync(string name, bool privateKey)
    {
        var connectionObj = GetConnection(name);

        var certificateConfig = connectionObj.Get<CertificateConfig>() ?? throw new Exception("connection name not found");
        var cert = CertificateLoader.LoadCertificate(certificateConfig, "certificate", null, privateKey);
        // var cert = new X509Certificate2(certificateConfig.Path ?? throw new ArgumentNullException(), certificateConfig.Password, X509KeyStorageFlags.DefaultKeySet)

        try
        {
            var result = new List<string>();
            if (cert != null)
            {
                static string format(X509Certificate2 cert)
                {
                    return $"{FormatCert(cert, cert.HasPrivateKey)}";
                }

                result.Add(format(cert));
                return Task.FromResult(VerifyResult.SuccessObject("Certificate", connectionObj, result));
            }
            else
            {
                return Task.FromResult<VerifyResult>(new VerifyFailed
                {
                    Success = false,
                    Error = "Certificate not found"
                });
            }

        }
        finally
        {
            cert?.Dispose();
        }
    }
}
