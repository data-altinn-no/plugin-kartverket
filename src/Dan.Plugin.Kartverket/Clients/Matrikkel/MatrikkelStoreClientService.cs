using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Matrikkel.StoreService;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel
{
    public class MatrikkelStoreClientService : IMatrikkelStoreClientService
    {
        // Veger, kretser, kommuner and code lists are public reference data that changes rarely
        // (kommune names ~yearly, code lists almost never) and is identical regardless of which
        // service context fetched it. They are cached across requests and contexts to avoid
        // repeated StoreService round trips for the same objects.
        private static readonly TimeSpan ReferenceDataTtl = TimeSpan.FromHours(24);

        private readonly ApplicationSettings _settings;
        private readonly ILogger _logger;
        private readonly IRequestContextService _requestContextService;
        private readonly IMemoryCache _cache;

        public MatrikkelStoreClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService, IMemoryCache cache)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelStoreClientService>();
            _requestContextService = requestContextService;
            _cache = cache;
        }

        public Task<Matrikkelenhet> GetMatrikkelenhet(long ident)
            => GetObject<Matrikkelenhet>(new MatrikkelenhetId { value = ident });

        public Task<Seksjon> GetMatrikkelenhetSeksjon(long ident)
            => GetObject<Seksjon>(new SeksjonId { value = ident });

        public Task<Adresse> GetAdresse(long ident)
            => GetObject<Adresse>(new AdresseId { value = ident });

        public Task<Veg> GetVeg(long ident)
            => GetCachedObject<Veg>(new VegId { value = ident });

        public Task<Krets> GetKrets(long ident)
            => GetCachedObject<Krets>(new KretsId { value = ident });

        public Task<Bygning> GetBygning(long bygningId)
            => GetObject<Bygning>(new BygningId { value = bygningId });

        public Task<Bruksenhet> GetBruksenhet(long ident)
            => GetObject<Bruksenhet>(new BruksenhetId { value = ident });

        public Task<Kommune> GetKommune(long ident)
            => GetCachedObject<Kommune>(new KommuneId { value = ident });

        public Task<BruksenhetstypeKode> GetBruksenhetstype(long bruksenhetstypeKodeId)
            => GetCachedObject<BruksenhetstypeKode>(new BruksenhetstypeKodeId { value = bruksenhetstypeKodeId });

        public Task<List<Bruksenhet>> GetBruksenheter(IEnumerable<long> idents)
            => GetObjects<Bruksenhet>(idents.Select(id => (MatrikkelBubbleId)new BruksenhetId { value = id }));

        public Task<List<Adresse>> GetAdresser(IEnumerable<long> idents)
            => GetObjects<Adresse>(idents.Select(id => (MatrikkelBubbleId)new AdresseId { value = id }));

        public Task<List<Bygning>> GetBygninger(IEnumerable<long> idents)
            => GetObjects<Bygning>(idents.Select(id => (MatrikkelBubbleId)new BygningId { value = id }));

        public Task<List<Veg>> GetVeger(IEnumerable<long> idents)
            => GetCachedObjects<Veg>(idents, id => new VegId { value = id });

        public Task<List<Krets>> GetKretser(IEnumerable<long> idents)
            => GetCachedObjects<Krets>(idents, id => new KretsId { value = id });

        public Task<List<BruksenhetstypeKode>> GetBruksenhetstyper(IEnumerable<long> idents)
            => GetCachedObjects<BruksenhetstypeKode>(idents, id => new BruksenhetstypeKodeId { value = id });

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
                await ((IClientChannel)client).CloseChannelAsync();
            }
        }

        /// <summary>
        /// Fetches several objects in a single StoreService round trip instead of one call per id.
        /// Ids that do not resolve to an object of type <typeparamref name="T"/> are silently omitted
        /// from the result (getObjectsIgnoreMissing semantics).
        /// </summary>
        private async Task<List<T>> GetObjects<T>(IEnumerable<MatrikkelBubbleId> ids) where T : class
        {
            var idArray = ids.ToArray();
            if (idArray.Length == 0)
                return new List<T>();

            var client = CreateClient();

            var request = new getObjectsIgnoreMissingRequest
            {
                matrikkelContext = GetContext(),
                ids = idArray
            };

            try
            {
                var response = await client.getObjectsIgnoreMissingAsync(request);
                return (response.@return ?? Array.Empty<MatrikkelBubbleObject>()).OfType<T>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk fetching {Count} {Type} objects from matrikkel store", idArray.Length, typeof(T).Name);
                return new List<T>();
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }
        }

        private async Task<T> GetCachedObject<T>(MatrikkelBubbleId id) where T : MatrikkelBubbleObject
        {
            if (_cache.TryGetValue(CacheKey<T>(id.value), out T cached))
                return cached;

            var result = await GetObject<T>(id);

            // Failed lookups are not cached, so transient errors do not stick for the TTL
            if (result != null)
                _cache.Set(CacheKey<T>(id.value), result, ReferenceDataTtl);

            return result;
        }

        private async Task<List<T>> GetCachedObjects<T>(IEnumerable<long> idents, Func<long, MatrikkelBubbleId> toBubbleId) where T : MatrikkelBubbleObject
        {
            var result = new List<T>();
            var missing = new List<long>();

            foreach (var ident in idents.Distinct())
            {
                if (_cache.TryGetValue(CacheKey<T>(ident), out T cached))
                    result.Add(cached);
                else
                    missing.Add(ident);
            }

            if (missing.Count > 0)
            {
                var fetched = await GetObjects<T>(missing.Select(toBubbleId));
                foreach (var item in fetched)
                {
                    _cache.Set(CacheKey<T>(item.id.value), item, ReferenceDataTtl);
                    result.Add(item);
                }
            }

            return result;
        }

        private static string CacheKey<T>(long ident) => $"matrikkelstore:{typeof(T).Name}:{ident}";

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<MatrikkelContext, Timestamp, KoordinatsystemKodeId>(_requestContextService.ServiceContext);
        }

        private StoreService CreateClient()
        {
            var endpointAddress = _settings.MatrikkelRootUrl + "StoreServiceWS";
            var serviceContext = _requestContextService.ServiceContext;

            return WcfChannelFactoryCache<StoreService>.CreateChannel(
                $"{endpointAddress}|{serviceContext.ToUpperInvariant()}",
                new EndpointAddress(endpointAddress),
                GrunnbokHelpers.GetBasicHttpBinding(),
                credentials => GrunnbokHelpers.SetMatrikkelWSCredentials(credentials, _settings, serviceContext));
        }
    }
}
