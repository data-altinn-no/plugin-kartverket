using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Matrikkel.KommuneService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ServiceModel;
using System.Threading.Tasks;


namespace Dan.Plugin.Kartverket.Clients.Matrikkel
{
    public class MatrikkelKommuneClientService : IMatrikkelKommuneClientService
    {
        private readonly ApplicationSettings _settings;
        private readonly ILogger _logger;
        private readonly IRequestContextService _requestContextService;
        public MatrikkelKommuneClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _logger = factory.CreateLogger <MatrikkelKommuneClientService>();
            _settings = settings.Value;
            _requestContextService = requestContextService;
        }

        public async Task<string> GetKommune(string kommunenummer)
        {
            var client = CreateClient();
            var request = new findGjeldendeKommuneIdForKommuneNrRequest()
            {
                matrikkelContext = GetContext(),
                kommuneNr = kommunenummer
            };
            try
            {
                var response = await client.findGjeldendeKommuneIdForKommuneNrAsync(request);

                return response.@return.value.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling KommuneServiceWS for kommuneNr {KommuneNr}", kommunenummer);
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }
            return string.Empty;
        }

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<MatrikkelContext, Timestamp, KoordinatsystemKodeId>(_requestContextService.ServiceContext);
        }

        private KommuneService CreateClient()
        {
            var endpointAddress = _settings.MatrikkelRootUrl + "KommuneServiceWS";
            var serviceContext = _requestContextService.ServiceContext;

            return WcfChannelFactoryCache<KommuneService>.CreateChannel(
                $"{endpointAddress}|{serviceContext}",
                new EndpointAddress(endpointAddress),
                GrunnbokHelpers.GetBasicHttpBinding(),
                credentials => GrunnbokHelpers.SetMatrikkelWSCredentials(credentials, _settings, serviceContext));
        }
    }
}
