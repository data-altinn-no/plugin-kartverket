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
        private ApplicationSettings _settings;
        private ILogger _logger;
        private readonly IRequestContextService _requestContextService;

        public StoreServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<StoreServiceClientService>();
            _requestContextService = requestContextService;
        }

        public async Task<KommuneDAN> GetKommune(string kommuneIdent)
        {
            KommuneDAN result = null;
            var client = CreateClient();

            var request = GetRequest();

            request.id = new KommuneId()
            {
                value = kommuneIdent
            };

            try
            {
                var storeServiceResponse = await client.getObjectAsync(request);
                var temp = (Kommune)storeServiceResponse.@return;
                result = new KommuneDAN()
                {
                    Name = temp.navn,
                    Number = temp.kommunenummer
                };
            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result;
        }

        public async Task<Registerenhetsrettsandel> GetRettighetsandeler(string id)
        {
            Registerenhetsrettsandel result = null;
            var client = CreateClient();

            var request = GetRequest();

            request.id = new RegisterenhetsrettsandelId()
            {
                value = id
            };

            try
            {
                var storeServiceResponse = await client.getObjectAsync(request);
                result = (Registerenhetsrettsandel)storeServiceResponse.@return;
            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result;
        }

        public async Task<Registerenhetsrett> GetRegisterenhetsrett(string id)
        {
            Registerenhetsrett result = null;
            var client = CreateClient();

            var request = GetRequest();

            request.id = new RegisterenhetsrettId()
            {
                value = id
            };

            try
            {
                var rettsendringer = await client.getObjectAsync(request);
                result = (Registerenhetsrett)rettsendringer.@return;
            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result;
        }

        public async Task<Rettsstiftelse> GetRettsstiftelse(string id)
        {
            Rettsstiftelse result = null;
            var client = CreateClient();

            var request = GetRequest();

            request.id = new RettsstiftelseId()
            {
                value = id
            };

            try
            {
                var storeServiceResponse = await client.getObjectAsync(request);
                result = (Rettsstiftelse)storeServiceResponse.@return;
            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result;
        }

        public async Task<Matrikkelenhet> GetRegisterenhet(string registerenhetid)
        {
            Matrikkelenhet result = null;
            var client = CreateClient();

            var request = GetRequest();

            request.id = new RegisterenhetId()
            {
                value = registerenhetid
            };

            try
            {
                var storeServiceResponse = await client.getObjectAsync(request);
                result = (Matrikkelenhet)storeServiceResponse.@return;
            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result;
        }

        public async Task<Dokument> GetDokument(string id)
        {
            Dokument result = null;
            var client = CreateClient();
            var request = GetRequest();

            request.id = new DokumentId()
            {
                value = id
            };

            try
            {
                var storeServiceResponse = await client.getObjectAsync(request);
                result = (Dokument)storeServiceResponse.@return;
            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result;
        }

        public async Task<List<PawnDocument>> GetPawnOwnerNames(List<PawnDocument> input)
        {
            var client = CreateClient();

            foreach (var inputItem in input)
            {
                var request = GetRequest();
                request.id = new PersonId()
                {
                    value = inputItem.OwnerId.ToString()
                };

                var response = await client.getObjectAsync(request);

                inputItem.Owner = ((Person)response.@return).navn;
            }

            return input;
        }

        public async Task<Matrikkelenhet> GetMatrikkelEnhetFromRegisterRettighetsandel(string registerenhetsrettId)
        {
            try
            {
                //var retter = await GetRettighetsandeler(registerrettighetsandelid);
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

        public async Task<Person> GetPerson(string personId)
        {
            var client = CreateClient();

            Person result = null;
            var request = GetRequest();
            request.id = new PersonId()
            {
                value = personId
            };
            try
            {
                var storeServiceResponse = await client.getObjectAsync(request);
                result = (Person)storeServiceResponse.@return;
            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return result;
        }
        private getObjectRequest GetRequest()
        {
            return new getObjectRequest()
            {
                grunnbokContext = GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext, Timestamp>(_requestContextService.ServiceContext)
            };
        }

        private StoreServiceClient CreateClient()
        {
            var serviceContext = _requestContextService.ServiceContext;

            if (string.IsNullOrWhiteSpace(serviceContext))
            {
                throw new InvalidOperationException(
                    "ServiceContext is not set. Ensure SetRequestContext() is called before using StoreServiceClientService.");
            }

            var binding = GrunnbokHelpers.GetBasicHttpBinding();

            var endpoint = new EndpointAddress(
                $"{_settings.GrunnbokRootUrl}StoreServiceWS");

            var client = new StoreServiceClient(binding, endpoint);

            GrunnbokHelpers.SetGrunnbokWSCredentials(
                client.ClientCredentials,
                _settings,
                serviceContext);

            return client;
        }

    }            
        
        public interface IStoreServiceClientService
        {
            public Task<KommuneDAN> GetKommune(string kommuneIdent);
            public Task<Registerenhetsrettsandel> GetRettighetsandeler(string id);

        public Task<Registerenhetsrett> GetRegisterenhetsrett(string id);

        public Task<Rettsstiftelse> GetRettsstiftelse(string id);

        public Task<Dokument> GetDokument(string id);

        public Task<List<PawnDocument>> GetPawnOwnerNames(List<PawnDocument> input);

        public Task<Matrikkelenhet> GetRegisterenhet(string registerenhetid);

        public Task<Matrikkelenhet> GetMatrikkelEnhetFromRegisterRettighetsandel(string registerrettighetsandelid);
        public Task<Person> GetPerson(string personId);
    }
}
