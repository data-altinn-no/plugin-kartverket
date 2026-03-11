namespace Dan.Plugin.Kartverket.Config
{
    public class KeyVaultConnectionStringSettings
    {
        public string AzureKeyVaultName { get; set; } = default!;
        public string SecretName { get; set; } = default!;
    }
}
