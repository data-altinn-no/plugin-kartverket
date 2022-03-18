using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Interfaces;

namespace Altinn.Dan.Plugin.Kartverket.Config
{
    public class KeyVaultClientDefinitionSettings : MaskinportenSettings
    {
        public string AzureKeyVaultName { get; set; }
        public string SecretName { get; set; }
    }

    public class KeyVaultClientDefinitionSettings<T> : KeyVaultClientDefinitionSettings where T : IClientDefinition { }
}
