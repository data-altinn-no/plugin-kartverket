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
            GrunnbokHelpers.SetCredentials(_client.ClientCredentials, _settings, ServiceContext.Matrikkel);
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

        public async Task<Adresse> GetAdresse(long ident)
        {
            getObjectResponse response = null;

            var request = new getObjectRequest()
            {
                matrikkelContext = GetContext(),
                id = new AdresseId()
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

            return (Adresse)response.@return;
        }

        public async Task<Veg> GetVeg(long ident)
        {
            getObjectResponse response = null;

            var request = new getObjectRequest()
            {
                matrikkelContext = GetContext(),
                id = new VegId()
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

            return (Veg)response.@return;
        }

        public async Task<Krets> GetKrets(long ident)
        {
            getObjectResponse response = null;

            var request = new getObjectRequest()
            {
                matrikkelContext = GetContext(),
                id = new KretsId()
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

            return (Krets)response.@return;
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

        public async Task<Bruksenhet> GetBruksenhet(long ident)
        {
            Bruksenhet result = null;
            var request = new getObjectRequest()
            {
                matrikkelContext = GetContext(),
                id = new BruksenhetId()
                {
                    value = ident
                }
            };

            try
            {
                var response = await _client.getObjectAsync(request);
                result = (Bruksenhet)response.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result;
        }

        public async Task<Kommune> GetKommune(long ident)
        {
            Kommune result = null;
            var request = new getObjectRequest()
            {
                matrikkelContext = GetContext(),
                id = new KommuneId()
                {
                    value = ident
                }
            };

            try
            {
                var response = await _client.getObjectAsync(request);
                result = (Kommune)response.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result;
        }

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<MatrikkelContext, Timestamp>();
        }
    }

    public interface IMatrikkelStoreClientService
    {
        public Task<Matrikkelenhet> GetMatrikkelenhet(long ident);
        public Task<Bygning> GetBygning(long bygningId);
        public Task<Seksjon> GetMatrikkelenhetSeksjon(long ident);

        public Task<Adresse> GetAdresse(long ident);

        public Task<Veg> GetVeg(long ident);

        public Task<Krets> GetKrets(long ident);

        public Task<Bruksenhet> GetBruksenhet(long ident);

        public Task<Kommune> GetKommune(long ident);
    }
}
