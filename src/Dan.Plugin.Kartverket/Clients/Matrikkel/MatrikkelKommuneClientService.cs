using Dan.Plugin.Kartverket.Clients.Grunnbok;
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
        private KommuneServiceClient _client;
        private readonly ApplicationSettings _settings;
        private readonly ILogger _logger;

        public MatrikkelKommuneClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _logger = factory.CreateLogger <MatrikkelKommuneClientService>();
            _settings = settings.Value;

            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();
            _client = new KommuneServiceClient(myBinding, new EndpointAddress(_settings.MatrikkelRootUrl + "KommuneServiceWS"));
            GrunnbokHelpers.SetMatrikkelWSCredentials(_client.ClientCredentials, _settings);
        }

        public async Task<string> GetKommune(string kommunenummer)
        {
            var request = new findGjeldendeKommuneIdForKommuneNrRequest()
            {
                matrikkelContext = GetContext(),
                kommuneNr = kommunenummer
            };

            var response = await _client.findGjeldendeKommuneIdForKommuneNrAsync(request);

            return response.@return.value.ToString();
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
                systemVersion = "eDueDiligence_1"
            };
        }
    }

    public interface IMatrikkelKommuneClientService
    {
        public Task<string> GetKommune(string kommunenummer);
    }
}
