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

        private BruksenhetServiceClient _client;

        public MatrikkelBruksenhetService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelBruksenhetService>();

            //Find ident for identifier
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            _client = new BruksenhetServiceClient(myBinding, new EndpointAddress(_settings.MatrikkelRootUrl + "BruksenhetServiceWS"));
            GrunnbokHelpers.SetMatrikkelWSCredentials(_client.ClientCredentials, _settings);
        }

        public async Task<long?> GetBruksenheter(long matrikkelEnhetId)
        {
            var request = new findBruksenheterForMatrikkelenhetRequest
            {
                matrikkelContext = GetContext(),
                matrikkelenhetId = new MatrikkelenhetId() { value = matrikkelEnhetId }
            };

            try
            {
                var response = await _client.findBruksenheterForMatrikkelenhetAsync(request);
                long? result = response.@return.FirstOrDefault()?.value;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return 0;
            }           
        }

        public async Task<string> GetAddressForBruksenhet(long bruksenhetId)
        {
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
            DateTime SNAPSHOT_VERSJON_DATO = new DateTime(9999, 1, 1, 0, 0, 0);

            return new MatrikkelContext()
            {
                locale = "no_NO",
                brukOriginaleKoordinater = true,
                koordinatsystemKodeId = new KoordinatsystemKodeId()
                {
                    value = 22
                },
                klientIdentifikasjon = "eDueDiligence",
                snapshotVersion = new Timestamp()
                {
                    timestamp = SNAPSHOT_VERSJON_DATO
                },
                systemVersion = "trunk"
            };
        }
    }

    public interface IMatrikkelBruksenhetService
    {
        Task<long?> GetBruksenheter(long matrikkelEnhetId);
        Task<string> GetAddressForBruksenhet(long bruksenhetId);
    }
}
