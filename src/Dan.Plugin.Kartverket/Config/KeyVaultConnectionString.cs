using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Config
{
    public class KeyVaultConnectionString
    {
        private readonly ILogger<KeyVaultConnectionString> _logger;
        private SecretClient _client;
        private KeyVaultConnectionStringSettings _settings;

        public KeyVaultConnectionString(ILogger<KeyVaultConnectionString> logger, IOptions<KeyVaultConnectionStringSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
            _client = new SecretClient(
                    new Uri($"https://{_settings.AzureKeyVaultName}.vault.azure.net/"),
                    new DefaultAzureCredential());
        }

        public async Task<string> GetConnectionString()
        {
            try
            {
                var secret = await _client.GetSecretAsync(_settings.SecretName);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching connection string from Key Vault");
                throw;
            }
        }
    }

}
