using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces;
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
        private readonly ILogger _logger;
        private readonly IRequestContextService _requestContextService;

        public MatrikkelStoreClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelStoreClientService>();
            _requestContextService = requestContextService;
        }

        public Task<Matrikkelenhet> GetMatrikkelenhet(long ident)
            => GetObject<Matrikkelenhet>(new MatrikkelenhetId { value = ident });

        public Task<Seksjon> GetMatrikkelenhetSeksjon(long ident)
            => GetObject<Seksjon>(new SeksjonId { value = ident });

        public Task<Adresse> GetAdresse(long ident)
            => GetObject<Adresse>(new AdresseId { value = ident });

        public Task<Veg> GetVeg(long ident)
            => GetObject<Veg>(new VegId { value = ident });

        public Task<Krets> GetKrets(long ident)
            => GetObject<Krets>(new KretsId { value = ident });

        public Task<Bygning> GetBygning(long bygningId)
            => GetObject<Bygning>(new BygningId { value = bygningId });

        public Task<Bruksenhet> GetBruksenhet(long ident)
            => GetObject<Bruksenhet>(new BruksenhetId { value = ident });

        public Task<Kommune> GetKommune(long ident)
            => GetObject<Kommune>(new KommuneId { value = ident });

        public Task<BruksenhetstypeKode> GetBruksenhetstype(long bruksenhetstypeKodeId)
            => GetObject<BruksenhetstypeKode>(new BruksenhetstypeKodeId { value = bruksenhetstypeKodeId });

        private async Task<T> GetObject<T>(MatrikkelBubbleId id) where T : class
        {
            var client = CreateClient();

            var request = new getObjectRequest
            {
                matrikkelContext = GetContext(),
                id = id
            };

            try
            {
                var response = await client.getObjectAsync(request);
                return (T)(object)response.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
            finally
            {
                try { client.Close(); }
                catch { client.Abort(); }
            }
        }

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<MatrikkelContext, Timestamp, KoordinatsystemKodeId>(_requestContextService.ServiceContext);
        }

        private StoreServiceClient CreateClient()
        {
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            var client = new StoreServiceClient(
                myBinding,
                new EndpointAddress(_settings.MatrikkelRootUrl + "StoreServiceWS")
            );

            GrunnbokHelpers.SetMatrikkelWSCredentials(
                client.ClientCredentials,
                _settings,
                _requestContextService.ServiceContext
            );

            return client;
        }
    }
}
