using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Models;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Config
{
    public class KeyVaultMaskinportenClientDefinition : IClientDefinition
    {
        private readonly ILogger<KeyVaultMaskinportenClientDefinition> _logger;

        public MaskinportenSettings ClientSettings { get; set; }
        public KeyVaultMaskinportenSettings KeyVaultMaskinportenSettings { get; set; } = new();
        private ClientSecrets _clientSecrets;

        public KeyVaultMaskinportenClientDefinition(ILogger<KeyVaultMaskinportenClientDefinition> logger)
        {
            _logger = logger;
        }

        public async Task<ClientSecrets> GetClientSecrets()
        {
            if (_clientSecrets != null)
            {
                return _clientSecrets;
            }

            var secretClient = new SecretClient(
                new Uri($"https://{KeyVaultMaskinportenSettings.AzureKeyVaultName}.vault.azure.net/"),
                new DefaultAzureCredential());

            var secret = await secretClient.GetSecretAsync(KeyVaultMaskinportenSettings.SecretName);
            var base64Str = secret.Value.Value;
            if (base64Str == null)
            {
                throw new ApplicationException("Unable to fetch cert from key vault");
            }

            var signingCertificate = new X509Certificate2(
                Convert.FromBase64String(base64Str),
                ClientSettings.CertificatePkcs12Password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            _clientSecrets = new ClientSecrets
            {
                ClientCertificate = signingCertificate
            };

            return _clientSecrets;
        }
    }
}
