using System;
using System.Threading.Tasks;
using Altinn.ApiClients.Maskinporten.Handlers;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Services;
using Altinn.Dan.Plugin.Kartverket.Clients;
using Altinn.Dan.Plugin.Kartverket.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;

namespace Altinn.Dan.Plugin.Kartverket
{
    class Program
    {
        private static ApplicationSettings ApplicationSettings { get; set; }

        private static Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    // See https://docs.microsoft.com/en-us/azure/azure-monitor/app/worker-service#using-application-insights-sdk-for-worker-services
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.AddHttpClient();

                    services.AddScoped<KartverketClient>();
                    services.AddScoped<ILandbrukClient, LandbrukClient>();
                    services.AddScoped<IAddressLookupClient, AddressLookupClient>();

                    services.AddOptions<ApplicationSettings>()
                        .Configure<IConfiguration>((settings, configuration) => configuration.Bind(settings));

                    var serviceProvider = services.BuildServiceProvider();
                    ApplicationSettings = serviceProvider.GetRequiredService<IOptions<ApplicationSettings>>().Value;

                    var registry = new PolicyRegistry()
                    {
                        { "defaultCircuitBreaker", HttpPolicyExtensions.HandleTransientHttpError().CircuitBreakerAsync(4, ApplicationSettings.BreakerRetryWaitTime) }
                    };
                    services.AddPolicyRegistry(registry);

                    services.AddMemoryCache();
                    services.AddSingleton<ITokenCacheProvider, MemoryTokenCacheProvider>();
                    services.AddSingleton<IMaskinportenService, MaskinportenService>();

                    //KartverketClient
                    services.Configure<KeyVaultClientDefinitionSettings<KeyVaultClientDefinition<IKartverketMaskinportenSettings>>>(serviceProvider
                        .GetRequiredService<IConfiguration>().GetSection("MPKartverket"));
                    services.AddSingleton<KeyVaultClientDefinition<IKartverketMaskinportenSettings>>();
                    services.AddTransient<MaskinportenTokenHandler<KeyVaultClientDefinition<IKartverketMaskinportenSettings>>>();
                    services.AddHttpClient("KartverketClient").AddHttpMessageHandler<MaskinportenTokenHandler<KeyVaultClientDefinition<IKartverketMaskinportenSettings>>>()
                        .AddPolicyHandlerFromRegistry("defaultCircuitBreaker");

                    //LandbrukClient
                    services.Configure<KeyVaultClientDefinitionSettings<KeyVaultClientDefinition<ILandbrukMaskinportenSettings>>>(serviceProvider
                        .GetRequiredService<IConfiguration>().GetSection("MP3Landbruk"));
                    services.AddSingleton<KeyVaultClientDefinition<ILandbrukMaskinportenSettings>>();
                    services.AddTransient<MaskinportenTokenHandler<KeyVaultClientDefinition<ILandbrukMaskinportenSettings>>>();
                    services.AddHttpClient("LandbrukClient").AddHttpMessageHandler<MaskinportenTokenHandler<KeyVaultClientDefinition<ILandbrukMaskinportenSettings>>>()
                        .AddPolicyHandlerFromRegistry("defaultCircuitBreaker");

                    // Client configured with circuit breaker policies
                    services.AddHttpClient("SafeHttpClient", client => { client.Timeout = new TimeSpan(0, 0, 30); })
                        .AddPolicyHandlerFromRegistry("defaultCircuitBreaker");

                    JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                    {
                        DefaultValueHandling = DefaultValueHandling.Ignore,
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    };
                })
                .Build();

            return host.RunAsync();
        }
    }

    internal interface IKartverketMaskinportenSettings : IClientDefinition
    {
    }

    internal interface ILandbrukMaskinportenSettings : IClientDefinition
    {
    }
}
