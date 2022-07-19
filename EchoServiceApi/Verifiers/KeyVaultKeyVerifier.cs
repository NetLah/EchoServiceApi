using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace EchoServiceApi.Verifiers
{
    public class KeyVaultKeyVerifier : BaseVerifier
    {
        private readonly ILogger _logger;
        private readonly DisagnosticInfo _myInfos;

        public KeyVaultKeyVerifier(IServiceProvider serviceProvider, ILogger<KeyVaultKeyVerifier> logger, DisagnosticInfo myInfos)
            : base(serviceProvider)
        {
            _myInfos = myInfos;
            _logger = logger;
        }

        public async Task<VerifyResult> VerifyAsync(string name)
        {
            var connectionObj = GetConnection(name);
            var vaultUri = new Uri(connectionObj.Value);

            var tokenCredential = await TokenFactory.GetTokenCredentialAsync();
            using var scope = _logger.BeginScope(_myInfos.LoggingScopeState);

            _logger.LogInformation("KeyVaultKeyVerifier: name={query_name}", name);

            var keyIdentifier = new Uri(vaultUri, "/");
            var locationParts = vaultUri.LocalPath.Split('/');
            if (locationParts.Length >= 3 && "keys" == locationParts[1])
            {
                var keyName = locationParts[2];
                var version = locationParts.Length >= 4 ? locationParts[3] : null;
                if (string.IsNullOrEmpty(version))
                    version = null;

                var keyClient = new KeyClient(keyIdentifier, tokenCredential);
                var response = await keyClient.GetKeyAsync(keyName, version);
                KeyVaultKey keyVaultKey = response.Value;

                var cryptographyClient = new CryptographyClient(vaultUri, tokenCredential);

                var text = Guid.NewGuid().ToString();
                var plaintext = Encoding.ASCII.GetBytes(text);
                var signedResult = await cryptographyClient.SignDataAsync(SignatureAlgorithm.RS256, plaintext);
                var signed = Convert.ToBase64String(signedResult.Signature);

                var encrypted = await cryptographyClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, plaintext);
                var ciphertext = Convert.ToBase64String(encrypted.Ciphertext);
                var decryptResult = await cryptographyClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, encrypted.Ciphertext);
                var decryptText = Encoding.ASCII.GetString(decryptResult.Plaintext);

                var rsa = keyVaultKey.Key.ToRSA();
                var validSignature = rsa.VerifyData(plaintext, signedResult.Signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                var detail = $"KeyType={keyVaultKey.KeyType}; Text={text}; Signature={signed}; Ciphertext={ciphertext}; Plaintext={decryptText}; ValidSignature={validSignature}";

                return VerifyResult.Successed("KeyVaultKey", connectionObj, detail: detail);
            }

            return new VerifyResult
            {
                Success = false,
                Detail = $"KeyVaultKey: invalid uri (3 parts): {vaultUri.LocalPath}",
            };
        }
    }
}
