using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces;
using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using Kartverket.Grunnbok.StoreService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using DokumentId = Kartverket.Grunnbok.StoreService.DokumentId;
using Kommune = Kartverket.Grunnbok.StoreService.Kommune;
using KommuneDAN = Dan.Plugin.Kartverket.Models.Kommune;
using KommuneId = Kartverket.Grunnbok.StoreService.KommuneId;
using Matrikkelenhet = Kartverket.Grunnbok.StoreService.Matrikkelenhet;
using PersonId = Kartverket.Grunnbok.StoreService.PersonId;
using RegisterenhetId = Kartverket.Grunnbok.StoreService.RegisterenhetId;
using RegisterenhetsrettId = Kartverket.Grunnbok.StoreService.RegisterenhetsrettId;
using RegisterenhetsrettsandelId = Kartverket.Grunnbok.StoreService.RegisterenhetsrettsandelId;
using RettsstiftelseId = Kartverket.Grunnbok.StoreService.RettsstiftelseId;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class StoreServiceClientService : IStoreServiceClientService
    {
        private readonly ApplicationSettings _settings;
        private readonly ILogger _logger;
        private readonly IRequestContextService _requestContextService;

        public StoreServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<StoreServiceClientService>();
            _requestContextService = requestContextService;
        }

        public async Task<KommuneDAN> GetKommune(string kommuneIdent)
        {
            var kommune = await GetObject<Kommune>(new KommuneId { value = kommuneIdent });

            return kommune == null
                ? null
                : new KommuneDAN
                {
                    Name = kommune.navn,
                    Number = kommune.kommunenummer
                };
        }

        public Task<Registerenhetsrettsandel> GetRettighetsandeler(string id)
            => GetObject<Registerenhetsrettsandel>(new RegisterenhetsrettsandelId { value = id });

        public Task<Registerenhetsrett> GetRegisterenhetsrett(string id)
            => GetObject<Registerenhetsrett>(new RegisterenhetsrettId { value = id });

        public Task<Rettsstiftelse> GetRettsstiftelse(string id)
            => GetObject<Rettsstiftelse>(new RettsstiftelseId { value = id });

        public Task<Matrikkelenhet> GetRegisterenhet(string registerenhetid)
            => GetObject<Matrikkelenhet>(new RegisterenhetId { value = registerenhetid });

        public Task<Dokument> GetDokument(string id)
            => GetObject<Dokument>(new DokumentId { value = id });

        public Task<Person> GetPerson(string personId)
            => GetObject<Person>(new PersonId { value = personId });

        public async Task<List<PawnDocument>> GetPawnOwnerNames(List<PawnDocument> input)
        {
            foreach (var inputItem in input)
            {
                var person = await GetObject<Person>(new PersonId { value = inputItem.OwnerId.ToString() });
                inputItem.Owner = person?.navn;
            }

            return input;
        }

        public async Task<Matrikkelenhet> GetMatrikkelEnhetFromRegisterRettighetsandel(string registerenhetsrettId)
        {
            try
            {
                var rrrett = await GetRegisterenhetsrett(registerenhetsrettId);
                var rregenhet = await GetRegisterenhet(rrrett.registerenhetId.value);

                return rregenhet;
            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return new Matrikkelenhet();
        }

        private async Task<T> GetObject<T>(GrunnbokBubbleObjectId id) where T : class
        {
            var client = CreateClient();

            var request = new getObjectRequest
            {
                grunnbokContext = GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext, Timestamp>(_requestContextService.ServiceContext),
                id = id
            };

            try
            {
                var response = await client.getObjectAsync(request);
                return (T)(object)response.@return;
            }
            // Grunnbok faults both for real failures and for not-found objects (e.g. a person that
            // no longer exists), and callers rely on null meaning "missing - skip". Returning null
            // is therefore kept deliberately; the full exception is logged for troubleshooting.
            catch (FaultException fex)
            {
                _logger.LogError(fex, "Grunnbok StoreService getObjectAsync fault for {IdType} {Id}", id?.GetType().Name, id?.value);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grunnbok StoreService getObjectAsync failed for {IdType} {Id}", id?.GetType().Name, id?.value);
                return null;
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }
        }

        private StoreService CreateClient()
        {
            var serviceContext = _requestContextService.ServiceContext;

            if (string.IsNullOrWhiteSpace(serviceContext))
            {
                throw new InvalidOperationException(
                    "ServiceContext is not set. Ensure SetRequestContext() is called before using StoreServiceClientService.");
            }

            var endpointAddress = $"{_settings.GrunnbokRootUrl}StoreServiceWS";

            return WcfChannelFactoryCache<StoreService>.CreateChannel(
                $"{endpointAddress}|{serviceContext.ToUpperInvariant()}",
                new EndpointAddress(endpointAddress),
                GrunnbokHelpers.GetBasicHttpBinding(),
                credentials => GrunnbokHelpers.SetGrunnbokWSCredentials(credentials, _settings, serviceContext));
        }
    }
}
