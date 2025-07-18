using Altinn.ApiClients.Maskinporten.Extensions;
using Dan.Common;
using Dan.Common.Extensions;
using Dan.Plugin.Kartverket;
using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Clients.Matrikkel;
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

            //Matrikkel og grunnbok services
            services.AddScoped<IKartverketGrunnbokMatrikkelService, KartverketGrunnbokMatrikkelService>();
            services.AddScoped<IIdentServiceClientService, IdentServiceClientService>();
            services.AddScoped<IStoreServiceClientService, StoreServiceClientService>();
            services.AddScoped<IMatrikkelenhetClientService, MatrikkelenhetClientService>();
            services.AddScoped<IMatrikkelKommuneClientService, MatrikkelKommuneClientService>();
            services.AddScoped<IRegisterenhetsRettsandelsServiceClientService, RegisterenhetsRettsandelsServiceClientService>();
            services.AddScoped<IMatrikkelStoreClientService, MatrikkelStoreClientService>();
            services.AddScoped<IMatrikkelPersonClientService, MatrikkelPersonClientService>();
            services.AddScoped<IOverfoeringServiceClientService, OverfoeringServiceClientService>();
            services.AddScoped<IRettsstiftelseClientService, RettsstiftelseClientService>();
            services.AddScoped<IRegisterenhetsrettClientService, RegisterenhetsrettClientService>();
            services.AddScoped<IInformasjonsServiceClientService, InformasjonsServiceClientService>();
            services.AddScoped<IMatrikkelBygningClientService, MatrikkelBygningClientService>();
            services.AddScoped<IMatrikkelBruksenhetService, MatrikkelBruksenhetService>();

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
        
    

