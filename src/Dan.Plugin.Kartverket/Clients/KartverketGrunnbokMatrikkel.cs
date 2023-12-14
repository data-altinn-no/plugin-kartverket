using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Kartverket.Grunnbok.IdentService;
using Kartverket.Grunnbok.RegisterenhetsrettsandelService;
using Kartverket.Grunnbok.StoreService;
using Kartverket.Grunnbok.RegisterenhetService;
using Microsoft.Extensions.Logging;
using RegisterenhetsrettsandelIdSS = Kartverket.Grunnbok.StoreService.RegisterenhetsrettsandelId;
using RegisterenhetsrettsandelIdRR = Kartverket.Grunnbok.RegisterenhetsrettsandelService.RegisterenhetsrettsandelId;
using Timestamp = Kartverket.Grunnbok.StoreService.Timestamp;
using GrunnbokContextIS = Kartverket.Grunnbok.IdentService.GrunnbokContext;
using RegisterenhetIdListRE = Kartverket.Grunnbok.RegisterenhetService.RegisterenhetIdList;
using RegisterenhetId = Kartverket.Grunnbok.StoreService.RegisterenhetId;
using RegisterenhetsrettsandelId = Kartverket.Grunnbok.StoreService.RegisterenhetsrettsandelId;
using RegisterenhetsrettId = Kartverket.Grunnbok.StoreService.RegisterenhetsrettId;

namespace Dan.Plugin.Kartverket.Clients
{
    public interface IKartverketGrunnbokMatrikkelService
    {
        //public Task<KartverketResponse> Get(string ssn);
        public Task<List<PropertyModel>> FindProperties(string identifier);
    }

    public class KartverketGrunnbokMatrikkelService : IKartverketGrunnbokMatrikkelService
    {

        private ApplicationSettings _settings;
        private ILogger _logger;
        private IIdentServiceClientService _identServiceClient;

        public KartverketGrunnbokMatrikkelService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IIdentServiceClientService identService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger(this.GetType().FullName);
            _identServiceClient = identService;
        }
        public async Task<List<PropertyModel>> FindProperties(string identifier)
        {
            var ident = await _identServiceClient.GetPersonIdentity(identifier);

            //Get properties ids
            var propertyids = await GetGrunnBokAndelerForRettighetshavere(ident);
            string testprop = propertyids.First().value;

            //Get actual properties
            var props = await GetGrunnbokProperties(testprop);

            var result = new List<PropertyModel>();
            result.Add(new PropertyModel());
            return result;
        }

        /*
        private async Task<int> GetRegisterenhetsSomething(string id)
        {
            //Find ident for identifier
            var myBinding = GetBasicHttpBinding();

            RegisterenhetServiceClient registerEnhetsserviceClient = new RegisterenhetServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokRootUrl + "RegisterenhetServiceWS"));
            SetGrunnbokWSCredentials(registerEnhetsserviceClient.ClientCredentials);

            var request = new findFestegrunnerForRequest()
            {
                Body = new findFestegrunnerForRequestBody()
                {
                    grunnbokContext = new GrunnbokContext()
                    {

                    },
                    registerenhetIds = new RegisterenhetIdListRE() { new RegisterenhetId { } }
                }
            }

            var response = await registerEnhetsserviceClient.findFestegrunnerForAsync(request);
        } */

        private async Task<Registerenhetsrettsandel> GetGrunnbokProperties(string propertyid)
        {
            //Find ident for identifier
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            StoreServiceClient storeservice = new StoreServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokRootUrl + "StoreServiceWS"));
            GrunnbokHelpers.SetGrunnbokWSCredentials(storeservice.ClientCredentials, _settings);

            var request = new getObjectRequest()
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
                id = new RegisterenhetsrettsandelId()
                {
                    value = propertyid
                }
            };

            try
            {
                var storeResponse = await storeservice.getObjectAsync(request);
                var retter = (Registerenhetsrettsandel) storeResponse.@return;

                var request2 = new getObjectRequest()
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
                    id = new RegisterenhetsrettId()
                    {
                        value = retter.registerenhetsrettId.value
                    }
                };

                var a = new RegisterenhetsrettId()
                {
                    value = 
                }
                var registerenhetsrett = await storeservice.getObjectAsync(request2);
                var rrrett = (Registerenhetsrett) registerenhetsrett.@return;

                var request3 = new getObjectRequest()
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
                    id = new RegisterenhetId()
                    {
                        value = rrrett.registerenhetId.value
                    }
                };

                var regenhet = await storeservice.getObjectAsync(request3);
                var rregenhet = (Registerenhet) regenhet.@return;


                var c = new getObjectRequest()
                {
                    id = new Kartverket.
                }

                return retter;

            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return new Registerenhetsrettsandel();
        }

        private async Task<List<RegisterenhetsrettsandelIdRR>> GetGrunnBokAndelerForRettighetshavere(string ident)
        {
            //Find ident for identifier
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();
            var retter = new List<RegisterenhetsrettsandelIdRR>();

            RegisterenhetsrettsandelServiceClient rettighetsservice = new RegisterenhetsrettsandelServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokRootUrl + "RegisterenhetsrettsandelServiceWS"));
           GrunnbokHelpers.SetGrunnbokWSCredentials(rettighetsservice.ClientCredentials, _settings);

            var request = new findAndelerForRettighetshavereRequest()
            {
                Body = new findAndelerForRettighetshavereRequestBody()
                {
                    personIds = new PersonIdList()
                    {
                        new()
                        {
                            value = ident
                        }
                    },
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
                    }
                }
            };

            try
            {
                var rettighetsresponse = await rettighetsservice.findAndelerForRettighetshavereAsync(request);
                retter = rettighetsresponse.Body.@return.SelectMany(d => d.Value).ToList();
              
            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return retter;
        }
    }
}
