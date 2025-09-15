using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Matrikkel.AdresseService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel
{
    public class MatrikkelAdresseClientService : IMatrikkelAdresseClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;

        private AdresseServiceClient _client;

        public MatrikkelAdresseClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelAdresseClientService>();

            //Find ident for identifier
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            _client = new AdresseServiceClient(myBinding, new EndpointAddress(_settings.MatrikkelRootUrl + "AdresseServiceWS"));
            GrunnbokHelpers.SetMatrikkelWSCredentials(_client.ClientCredentials, _settings);
        }

        public async Task<AdresseId[]> GetAdresserForMatrikkelenhet(long matrikkelEnhetId)
        {
            var request = new findAdresserForMatrikkelenhetRequest
            {
                matrikkelContext = GetContext(),
                matrikkelenhetId = new MatrikkelenhetId() { value = matrikkelEnhetId }
            };

            try
            {
                var response = await _client.findAdresserForMatrikkelenhetAsync(request);
                var result = response.@return;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Array.Empty<AdresseId>();
            }
        }

        public async Task<AdresseId[]> FindAdresser(string adresseNavn, string kommuneNo)
        {
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
                var response = await _client.findAdresserAsync(request);
                return response.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
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

    public interface IMatrikkelAdresseClientService
    {
        Task<AdresseId[]> GetAdresserForMatrikkelenhet(long matrikkelEnhetId);
        Task<AdresseId[]> FindAdresser(string adresseNavn, string kommuneNo);
    }
}
