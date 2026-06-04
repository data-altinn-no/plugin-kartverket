using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces;
using Dan.Plugin.Kartverket.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Kartverket.Matrikkel.BygningService;
using MatrikkelenhetId = Kartverket.Matrikkel.BygningService.MatrikkelenhetId;
using MatrikkelContext = Kartverket.Matrikkel.BygningService.MatrikkelContext;
using Timestamp = Kartverket.Matrikkel.BygningService.Timestamp;
using KoordinatsystemKodeId = Kartverket.Matrikkel.BygningService.KoordinatsystemKodeId;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel
{
    public class MatrikkelBygningClientService : IMatrikkelBygningClientService
    {
        private readonly ApplicationSettings _settings;
        private readonly ILogger _logger;
        private readonly IRequestContextService _requestContextService;
        public MatrikkelBygningClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelBygningClientService>();
            _requestContextService = requestContextService; 
        }

        public async Task<List<long>> GetBygningerForMatrikkelenhet(long matrikkelEnhetId)
        {
            List<long> result = new List<long>();
            var client = CreateClient();

            var request = new findByggForMatrikkelenhetRequest()
            {
                matrikkelContext = GetContext(),
                matrikkelenhetId = new MatrikkelenhetId()
                {
                    value = matrikkelEnhetId
                }
            };

            try
            {
                var response = await client.findByggForMatrikkelenhetAsync(request);
                result.AddRange(response.@return.Select(x=>x.value).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }

            return result;
        }

        public async Task<findAlleBygningstypeKoderResponse> GetBygningsType()
        {
            var result = new findAlleBygningstypeKoderResponse();

            var client = CreateClient();

            var request = new findAlleBygningstypeKoderRequest()
            {
                matrikkelContext = GetContext(),                
            };

            try
            {
                result = await client.findAlleBygningstypeKoderAsync(request);
            }
            catch
            {
                _logger.LogError($"Feil ved innhenting av bygningstype");
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }
            return result;
        }

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<MatrikkelContext, Timestamp, KoordinatsystemKodeId>(_requestContextService.ServiceContext);
        }

        private BygningService CreateClient()
        {
            var endpointAddress = _settings.MatrikkelRootUrl + "BygningServiceWS";
            var serviceContext = _requestContextService.ServiceContext;

            return WcfChannelFactoryCache<BygningService>.CreateChannel(
                $"{endpointAddress}|{serviceContext.ToUpperInvariant()}",
                new EndpointAddress(endpointAddress),
                GrunnbokHelpers.GetBasicHttpBinding(),
                credentials => GrunnbokHelpers.SetMatrikkelWSCredentials(credentials, _settings, serviceContext));
        }

    }
}
