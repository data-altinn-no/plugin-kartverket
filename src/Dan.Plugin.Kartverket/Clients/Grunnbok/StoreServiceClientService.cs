using Azure.Core;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Grunnbok.StoreService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Dan.Plugin.Kartverket.Models;
using KommuneId = Kartverket.Grunnbok.StoreService.KommuneId;
using KommuneDAN = Dan.Plugin.Kartverket.Models.Kommune;
using Kommune = Kartverket.Grunnbok.StoreService.Kommune;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class StoreServiceClientService: IStoreServiceClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;

        private StoreServiceClient _client;

        public StoreServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<StoreServiceClientService>();

            //Find ident for identifier
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            _client = new StoreServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokRootUrl + "StoreServiceWS"));
            GrunnbokHelpers.SetGrunnbokWSCredentials(_client.ClientCredentials, _settings);
        }

        private getObjectRequest GetRequest()
        {
            return new getObjectRequest()
            {
                grunnbokContext = new()
                {
                    clientIdentification = "eDueDiligence",
                    clientTraceInfo = "eDueDiligence_1",
                    locale = "no_578",
                    snapshotVersion = new()
                    {
                        timestamp = new DateTime(9999, 1, 1, 0, 0, 0)
                    },
                    systemVersion = "1"
                },
                id = null
            };
        }

        public async Task<KommuneDAN> GetKommune(string kommuneIdent)
        {

            KommuneDAN result = null;

            var request = GetRequest();

            request.id = new KommuneId()
            {
                value = kommuneIdent
            };

            try
            {
                var storeServiceResponse = await _client.getObjectAsync(request);
                var temp = (Kommune) storeServiceResponse.@return;
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

            var request = GetRequest();

            request.id = new RegisterenhetsrettsandelId()
            {
                value = id
            };

            try
            {
                var storeServiceResponse = await _client.getObjectAsync(request);
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

            var request = GetRequest();

            request.id = new RegisterenhetsrettId()
            {
                value = id
            };

            try
            {
                var storeServiceResponse = await _client.getObjectAsync(request);
                result = (Registerenhetsrett)storeServiceResponse.@return;
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

            var request = GetRequest();

            request.id = new RettsstiftelseId()
            {
                value = id
            };

            try
            {
                var storeServiceResponse = await _client.getObjectAsync(request);
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

        public async Task<Dokument> GetDokument(string id)
        {
            Dokument result = null;

            var request = GetRequest();

            request.id = new DokumentId()
            {
                value = id
            };

            try
            {
                var storeServiceResponse = await _client.getObjectAsync(request);
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

            foreach (var inputItem in input)
            {
                var request = GetRequest();
                request.id = new PersonId()
                {
                    value = inputItem.OwnerId.ToString()
                };

                var response = await _client.getObjectAsync(request);

                inputItem.Owner = ((Person)response.@return).navn;
            }

            return input;
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
    }
}
