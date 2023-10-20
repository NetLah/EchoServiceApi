using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

namespace EchoServiceApi.Verifiers;

public class KeyVaultCertificateVerifier : BaseVerifier
{
    public KeyVaultCertificateVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

    private static string FormatCert(X509Certificate2 cert, bool privateKey) => $"Subject={cert.Subject}; Expires={cert.GetExpirationDateString()}; HasPrivateKey={privateKey}";

    public async Task<VerifyResult> VerifyAsync(string name, bool privateKey, bool all)
    {
        var connectionObj = GetConnection(name);

        var connectionCredentialValue = connectionObj.TryGet<ConnectionCredentialValue>();
        var tokenCredential = await TokenFactory.GetTokenCredentialOrDefaultAsync(connectionCredentialValue);
        var vaultUri = new Uri(connectionCredentialValue.Value ?? throw new NullReferenceException("value is required"));

        var certificateIdentifier = new Uri(vaultUri, "/");
        var locationParts = vaultUri.LocalPath.Split('/');
        X509Certificate2 cert;

        using var scope = LoggerBeginScopeDiagnostic();

        Logger.LogInformation("KeyVaultCertificateVerifier: name={query_name} privateKey={query_privateKey}", name, privateKey);

        var result = new List<string>();

        if (locationParts.Length >= 3 && "certificates" == locationParts[1])
        {
            var certificateName = locationParts[2];
            var versions = new List<string?>();
            var version = locationParts.Length >= 4 ? locationParts[3] : null;
            if (string.IsNullOrEmpty(version))
            {
                version = null;
            }

            versions.Add(version);

            if (privateKey)
            {
                var secretClient = new SecretClient(certificateIdentifier, tokenCredential);

                if (all)
                {
                    versions.Clear();
                    await foreach (var item in secretClient.GetPropertiesOfSecretVersionsAsync(certificateName))
                    {
                        if (item.Enabled != false)
                        {
                            versions.Add(item.Version);
                        }
                    }
                }

                foreach (var item in versions)
                {
                    var response = await secretClient.GetSecretAsync(certificateName, item);
                    KeyVaultSecret keyVaultSecret = response.Value;
                    var bytes = Convert.FromBase64String(keyVaultSecret.Value);
                    cert = new X509Certificate2(bytes, (string?)null, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
                    string format(X509Certificate2 cert) => $"Version={keyVaultSecret.Properties.Version}; Enabled={keyVaultSecret.Properties.Enabled}; {FormatCert(cert, privateKey)}; Length={bytes.Length}";
                    result.Add(format(cert));
                }
            }
            else
            {
                var certificateClient = new CertificateClient(certificateIdentifier, tokenCredential);
                if (all)
                {
                    versions.Clear();
                    await foreach (var item in certificateClient.GetPropertiesOfCertificateVersionsAsync(certificateName))
                    {
                        versions.Add(item.Version);
                    }
                }

                foreach (var item in versions)
                {
                    var keyVaultCertificate = item == null ?
                        (await certificateClient.GetCertificateAsync(certificateName)).Value :
                        (await certificateClient.GetCertificateVersionAsync(certificateName, item)).Value;
                    cert = new X509Certificate2(keyVaultCertificate.Cer);
                    string format(X509Certificate2 cert) => $"Version={keyVaultCertificate.Properties.Version}; Enabled={keyVaultCertificate.Properties.Enabled}; {FormatCert(cert, privateKey)}; Length={keyVaultCertificate.Cer.Length}";
                    result.Add(format(cert));
                }
            }
        }

        return VerifyResult.SuccessObject("KeyVaultCertificate", connectionObj, result);
    }
}
