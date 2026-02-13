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
using static Dan.Plugin.Kartverket.Clients.Grunnbok.StoreServiceClientService;

var host = new HostBuilder()
        .ConfigureDanPluginDefaults()
        .ConfigureServices((context, services) =>
        {
            var configurationRoot = context.Configuration;
            services.Configure<ApplicationSettings>(configurationRoot);
            services.AddTransient<IAddressLookupClient, AddressLookupClient>();
            services.AddTransient<IDDWrapper, DDWrapper>();
            services.AddTransient<IDiHeWrapper, DiHeWrapper>();

            //Matrikkel og grunnbok services
            services.AddTransient<IKartverketGrunnbokMatrikkelService, KartverketGrunnbokMatrikkelService>();
            services.AddTransient<IIdentServiceClientService, IdentServiceClientService>();
            services.AddTransient<IStoreServiceClientService, StoreServiceClientService>();
            services.AddTransient<IMatrikkelenhetClientService, MatrikkelenhetClientService>();
            services.AddTransient<IMatrikkelKommuneClientService, MatrikkelKommuneClientService>();
            services.AddTransient<IRegisterenhetsRettsandelsServiceClientService, RegisterenhetsRettsandelsServiceClientService>();
            services.AddTransient<IMatrikkelStoreClientService, MatrikkelStoreClientService>();
            services.AddTransient<IMatrikkelPersonClientService, MatrikkelPersonClientService>();
            services.AddTransient<IOverfoeringServiceClientService, OverfoeringServiceClientService>();
            services.AddTransient<IRettsstiftelseClientService, RettsstiftelseClientService>();
            services.AddTransient<IRegisterenhetsrettClientService, RegisterenhetsrettClientService>();
            services.AddTransient<IInformasjonsServiceClientService, InformasjonsServiceClientService>();
            services.AddTransient<IMatrikkelBygningClientService, MatrikkelBygningClientService>();
            services.AddTransient<IMatrikkelBruksenhetService, MatrikkelBruksenhetService>();
            services.AddTransient<IMatrikkelAdresseClientService, MatrikkelAdresseClientService>();
            services.AddScoped<IRequestContextService, RequestContextService>();


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
        
    

