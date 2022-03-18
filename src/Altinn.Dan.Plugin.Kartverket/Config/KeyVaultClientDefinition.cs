using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Models;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;

namespace Altinn.Dan.Plugin.Kartverket.Config
{
    public class KeyVaultClientDefinition : IClientDefinition
    {
        public MaskinportenSettings ClientSettings { get; set; }
        public KeyVaultClientDefinitionSettings KeyVaultClientDefinitionSettings { get; set; }
        private ClientSecrets _clientSecrets;

        protected KeyVaultClientDefinition()
        {
        }

        public KeyVaultClientDefinition(IOptions<KeyVaultClientDefinitionSettings> clientSettings)
        {
            ClientSettings = clientSettings.Value;
            KeyVaultClientDefinitionSettings = clientSettings.Value;
        }

        public async Task<ClientSecrets> GetClientSecrets()
        {
            if (_clientSecrets != null)
            {
                return _clientSecrets;
            }

            var secretClient = new SecretClient(
                new Uri($"https://{KeyVaultClientDefinitionSettings.AzureKeyVaultName}.vault.azure.net/"),
                new DefaultAzureCredential());

            var secret = await secretClient.GetSecretAsync(KeyVaultClientDefinitionSettings.SecretName);
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

    public class KeyVaultClientDefinition<T> : KeyVaultClientDefinition where T : IClientDefinition
    {
        public KeyVaultClientDefinition(IOptions<KeyVaultClientDefinitionSettings<KeyVaultClientDefinition<T>>> clientSettings)
        {
            ClientSettings = clientSettings.Value;
            KeyVaultClientDefinitionSettings = clientSettings.Value;
        }
    }
}
