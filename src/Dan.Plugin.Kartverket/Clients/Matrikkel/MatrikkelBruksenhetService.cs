using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Kartverket.Matrikkel.BruksenhetService;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel
{
    public class MatrikkelBruksenhetService : IMatrikkelBruksenhetService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;
        private IRequestContextService _requestContextService;

        public MatrikkelBruksenhetService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelBruksenhetService>();
            _requestContextService = requestContextService;
        }

        public async Task<BruksenhetId[]> GetBruksenheter(long matrikkelEnhetId)
        {
            var _client = CreateClient();
            var request = new findBruksenheterForMatrikkelenhetRequest
            {
                matrikkelContext = GetContext(),
                matrikkelenhetId = new MatrikkelenhetId() { value = matrikkelEnhetId }
            };

            try
            {
                var response = await _client.findBruksenheterForMatrikkelenhetAsync(request);
                var result = response.@return;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Array.Empty<BruksenhetId>();
            }           
        }

        public async Task<string> GetAddressForBruksenhet(long bruksenhetId)
        {
            var _client = CreateClient();

            var request = new findOffisiellAdresseForBruksenhetRequest
            {
                matrikkelContext = GetContext(),
                bruksenhetId = new BruksenhetId { value = bruksenhetId }
            };

            try
            {
                var response = await _client.findOffisiellAdresseForBruksenhetAsync(request);
                return response.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return string.Empty;
            }
        }

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<MatrikkelContext, Timestamp>();
        }

        private BruksenhetServiceClient CreateClient()
        {
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            var client = new BruksenhetServiceClient(
                myBinding,
                new EndpointAddress(_settings.MatrikkelRootUrl + "BruksenhetServiceWS")
            );

            GrunnbokHelpers.SetMatrikkelWSCredentials(
                client.ClientCredentials,
                _settings,
                _requestContextService.ServiceContext
            );

            return client;
        }
    }

    public interface IMatrikkelBruksenhetService
    {
        Task<BruksenhetId[]> GetBruksenheter(long matrikkelEnhetId);
        Task<string> GetAddressForBruksenhet(long bruksenhetId);
    }
}
