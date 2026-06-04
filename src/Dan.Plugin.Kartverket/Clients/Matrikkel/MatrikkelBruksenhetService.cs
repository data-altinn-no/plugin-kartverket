using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Matrikkel.BruksenhetService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ServiceModel;
using System.Threading.Tasks;
using MatrikkelContext = Kartverket.Matrikkel.BruksenhetService.MatrikkelContext;
using KoordinatsystemKodeId = Kartverket.Matrikkel.BruksenhetService.KoordinatsystemKodeId;
using Timestamp = Kartverket.Matrikkel.BruksenhetService.Timestamp;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel
{
    public class MatrikkelBruksenhetService : IMatrikkelBruksenhetService
    {
        private readonly ApplicationSettings _settings;
        private readonly ILogger _logger;
        private readonly IRequestContextService _requestContextService;

        public MatrikkelBruksenhetService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelBruksenhetService>();
            _requestContextService = requestContextService;
        }

        public async Task<BruksenhetId[]> GetBruksenheter(long matrikkelEnhetId)
        {
            var client = CreateClient();
            var request = new findBruksenheterForMatrikkelenhetRequest
            {
                matrikkelContext = GetContext(),
                matrikkelenhetId = new MatrikkelenhetId() { value = matrikkelEnhetId }
            };

            try
            {
                var response = await client.findBruksenheterForMatrikkelenhetAsync(request);
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
            return Array.Empty<BruksenhetId>();
        }

        public async Task<string> GetAddressForBruksenhet(long bruksenhetId)
        {
            var client = CreateClient();

            var request = new findOffisiellAdresseForBruksenhetRequest
            {
                matrikkelContext = GetContext(),
                bruksenhetId = new BruksenhetId { value = bruksenhetId }
            };

            try
            {
                var response = await client.findOffisiellAdresseForBruksenhetAsync(request);
                return response.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return string.Empty;
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }
        }

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<MatrikkelContext, Timestamp, KoordinatsystemKodeId>(_requestContextService.ServiceContext);
        }

        private BruksenhetService CreateClient()
        {
            var endpointAddress = _settings.MatrikkelRootUrl + "BruksenhetServiceWS";
            var serviceContext = _requestContextService.ServiceContext;

            return WcfChannelFactoryCache<BruksenhetService>.CreateChannel(
                $"{endpointAddress}|{serviceContext.ToUpperInvariant()}",
                new EndpointAddress(endpointAddress),
                GrunnbokHelpers.GetBasicHttpBinding(),
                credentials => GrunnbokHelpers.SetMatrikkelWSCredentials(credentials, _settings, serviceContext));
        }

    }
}
