using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Matrikkel.StoreService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel
{
    public class MatrikkelStoreClientService : IMatrikkelStoreClientService
    {
        private readonly ApplicationSettings _settings;
        private ILogger _logger;
        private StoreServiceClient _client;


        public MatrikkelStoreClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelStoreClientService>();

            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            _client = new StoreServiceClient(myBinding, new EndpointAddress(_settings.MatrikkelRootUrl + "StoreServiceWS"));
            GrunnbokHelpers.SetMatrikkelWSCredentials(_client.ClientCredentials, _settings);
        }

        public async Task<Matrikkelenhet> GetMatrikkelenhet(long ident)
        {
            getObjectResponse response = null;

            var request = new getObjectRequest()
            {
                matrikkelContext = GetContext(),
                id = new MatrikkelenhetId()
                {
                    value = ident
                }
            };

            try
            {                
                response = await _client.getObjectAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return (Matrikkelenhet) response.@return;
        }
        public async Task<Seksjon> GetMatrikkelenhetSeksjon(long ident)
        {
            getObjectResponse response = null;

            var request = new getObjectRequest()
            {
                matrikkelContext = GetContext(),
                id = new SeksjonId()
                {
                    value = ident
                }
            };

            try
            {
                response = await _client.getObjectAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return (Seksjon)response.@return;
        }

        public async Task<Bygning> GetBygning(long bygningId)
        {
            Bygning result = null;
            var request = new getObjectRequest()
            {
                matrikkelContext = GetContext(),
                id = new BygningId()
                {
                    value = bygningId
                }
            };

            try
            {
                var response = await _client.getObjectAsync(request);
                result = (Bygning)response.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result;

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

    public interface IMatrikkelStoreClientService
    {
        public Task<Matrikkelenhet> GetMatrikkelenhet(long ident);
        public Task<Bygning> GetBygning(long bygningId);
        public Task<Seksjon> GetMatrikkelenhetSeksjon(long ident);
    }
}
