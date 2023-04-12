using Altinn.ApiClients.Maskinporten.Extensions;
using Dan.Common;
using Dan.Common.Extensions;
using Dan.Plugin.Kartverket;
using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
        .ConfigureDanPluginDefaults()
        .ConfigureServices((context, services) =>
        {
            var configurationRoot = context.Configuration;
            services.Configure<ApplicationSettings>(configurationRoot);
            services.AddScoped<IAddressLookupClient, AddressLookupClient>();
            services.AddTransient<IDDWrapper, DDWrapper>();

            //KartverketClient
            services.AddMaskinportenHttpClient<KeyVaultMaskinportenClientDefinition, KartverketClient>(configurationRoot.GetSection("MPKartverket"),
    clientDefinition =>
                {
                    configurationRoot.GetSection("MPKartverket").Bind(clientDefinition.KeyVaultMaskinportenSettings);
                })
                .AddPolicyHandlerFromRegistry(Constants.SafeHttpClientPolicy);

            //LandbrukClient
            services.AddMaskinportenHttpClient<KeyVaultMaskinportenClientDefinition, LandbrukClient>(configurationRoot.GetSection("MP3Landbruk"),
                clientDefinition =>
                {
                    configurationRoot.GetSection("MP3Landbruk").Bind(clientDefinition.KeyVaultMaskinportenSettings);
                })
                .AddPolicyHandlerFromRegistry(Constants.SafeHttpClientPolicy);

        })
        .Build();

    await host.RunAsync();
        
    

