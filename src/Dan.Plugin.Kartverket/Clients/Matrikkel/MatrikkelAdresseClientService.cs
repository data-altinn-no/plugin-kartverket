using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Matrikkel.AdresseService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ServiceModel;
using System.Threading.Tasks;
using MatrikkelContext = Kartverket.Matrikkel.AdresseService.MatrikkelContext;
using KoordinatsystemKodeId = Kartverket.Matrikkel.AdresseService.KoordinatsystemKodeId;
using Timestamp = Kartverket.Matrikkel.AdresseService.Timestamp;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel
{
    public class MatrikkelAdresseClientService : IMatrikkelAdresseClientService
    {
        private readonly ApplicationSettings _settings;
        private readonly ILogger _logger;
        private readonly IRequestContextService _requestContextService;

        public MatrikkelAdresseClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelAdresseClientService>();
            _requestContextService = requestContextService;
        }

        public async Task<AdresseId[]> GetAdresserForMatrikkelenhet(long matrikkelEnhetId)
        {
            var client = CreateClient();
            var request = new findAdresserForMatrikkelenhetRequest
            {
                matrikkelContext = GetContext(),
                matrikkelenhetId = new MatrikkelenhetId() { value = matrikkelEnhetId }
            };

            try
            {
                var response = await client.findAdresserForMatrikkelenhetAsync(request);
                var result = response.@return;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }

            return Array.Empty<AdresseId>();
        }

        public async Task<AdresseId[]> FindAdresser(string adresseNavn, string kommuneNo)
        {
            var client = CreateClient();
            var request = new findAdresserRequest
            {
                matrikkelContext = GetContext(),
                adressesokModel = new AdressesokModel()
                {
                    adressenavn = adresseNavn,
                    kommunenummer = kommuneNo
                }
            };
            try
            {
                var response = await client.findAdresserAsync(request);
                return response.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }
            return Array.Empty<AdresseId>();
        }

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<
                MatrikkelContext,
                Timestamp,
                KoordinatsystemKodeId>
                (_requestContextService.ServiceContext);
        }

        private AdresseService CreateClient()
        {
            var endpointAddress = _settings.MatrikkelRootUrl + "AdresseServiceWS";
            var serviceContext = _requestContextService.ServiceContext;

            return WcfChannelFactoryCache<AdresseService>.CreateChannel(
                $"{endpointAddress}|{serviceContext}",
                new EndpointAddress(endpointAddress),
                GrunnbokHelpers.GetBasicHttpBinding(),
                credentials => GrunnbokHelpers.SetMatrikkelWSCredentials(credentials, _settings, serviceContext));
        }

    }
}
