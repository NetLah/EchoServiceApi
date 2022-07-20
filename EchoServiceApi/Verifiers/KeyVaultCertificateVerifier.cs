using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

namespace EchoServiceApi.Verifiers
{
    public class KeyVaultCertificateVerifier : BaseVerifier
    {
        public KeyVaultCertificateVerifier(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<VerifyResult> VerifyAsync(string name, bool privateKey)
        {
            var connectionObj = GetConnection(name);

            var connectionCredentialValue = connectionObj.TryGet<ConnectionCredentialValue>();
            var tokenCredential = await TokenFactory.GetTokenCredentialOrDefaultAsync(connectionCredentialValue);
            var vaultUri = new Uri(connectionCredentialValue.Value ?? throw new NullReferenceException("value is required"));

            var certificateIdentifier = new Uri(vaultUri, "/");
            var locationParts = vaultUri.LocalPath.Split('/');
            X509Certificate2? cert = null;
            Func<X509Certificate2?, string> factory = cert => $"Subject={cert?.Subject}; Expires={cert?.GetExpirationDateString()}; HasPrivateKey={privateKey}";

            using var scope = LoggerBeginScopeDiagnostic();

            Logger.LogInformation("KeyVaultCertificateVerifier: name={query_name} privateKey={query_privateKey}", name, privateKey);

            if (locationParts.Length >= 3 && "certificates" == locationParts[1])
            {
                var certificateName = locationParts[2];
                var version = locationParts.Length >= 4 ? locationParts[3] : null;
                if (string.IsNullOrEmpty(version))
                    version = null;

                if (privateKey)
                {
                    var secretClient = new SecretClient(certificateIdentifier, tokenCredential);
                    var response = await secretClient.GetSecretAsync(certificateName, version);
                    KeyVaultSecret keyVaultSecret = response.Value;
                    var bytes = Convert.FromBase64String(keyVaultSecret.Value);
                    cert = new X509Certificate2(bytes, (string?)null, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
                    var previous = factory;
                    factory = cert => $"{previous(cert)}; Length={bytes.Length}";
                }
                else
                {
                    var certificateClient = new CertificateClient(certificateIdentifier, tokenCredential);
                    KeyVaultCertificate response = string.IsNullOrEmpty(version) ?
                        certificateClient.GetCertificate(certificateName).Value :
                        certificateClient.GetCertificateVersion(certificateName, version).Value;

                    cert = new X509Certificate2(response.Cer);
                }
            }

            var detail = factory(cert);
            return VerifyResult.Successed("KeyVaultCertificate", connectionObj,
                detail: detail);
        }
    }
}
