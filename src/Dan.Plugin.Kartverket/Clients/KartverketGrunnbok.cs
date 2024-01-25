using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Clients.Matrikkel;
using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using Kartverket.Grunnbok.RegisterenhetsrettsandelService;
using Kartverket.Grunnbok.StoreService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Kartverket.Matrikkel.StoreService;
using Namotion.Reflection;
using getObjectRequest = Kartverket.Grunnbok.StoreService.getObjectRequest;
using Matrikkelenhet = Kartverket.Grunnbok.StoreService.Matrikkelenhet;
using PersonId = Kartverket.Grunnbok.RegisterenhetsrettsandelService.PersonId;
using RegisterenhetId = Kartverket.Grunnbok.StoreService.RegisterenhetId;
using RegisterenhetsrettId = Kartverket.Grunnbok.StoreService.RegisterenhetsrettId;
using RegisterenhetsrettsandelId = Kartverket.Grunnbok.StoreService.RegisterenhetsrettsandelId;
using RegisterenhetsrettsandelIdRR = Kartverket.Grunnbok.RegisterenhetsrettsandelService.RegisterenhetsrettsandelId;
using OverdragelseAvRegisterenhetsrett = Kartverket.Grunnbok.InformasjonsService.OverdragelseAvRegisterenhetsrett;
using StoreServiceClient = Kartverket.Grunnbok.StoreService.StoreServiceClient;
using Teig = Kartverket.Matrikkel.MatrikkelenhetService.Teig;

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
        private IStoreServiceClientService _storeServiceClient;
        private IMatrikkelenhetClientService _matrikkelenhetServiceClient;
        private IMatrikkelKommuneClientService _matrikkelKommuneClient;
        private IMatrikkelStoreClientService _matrikkelStoreClient;
        private IMatrikkelPersonClientService _matrikkelPersonClient;
        private IOverfoeringServiceClientService _overfoeringsClientService;
        private IRettsstiftelseClientService _rettsstiftelseClientService;
        private IRegisterenhetsrettClientService _registerenhetsrettClientService;
        private IInformasjonsServiceClientService _informasjonsServiceClientService;

        public KartverketGrunnbokMatrikkelService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IIdentServiceClientService identService, IStoreServiceClientService storeService, IMatrikkelenhetClientService matrikkelService,
            IMatrikkelKommuneClientService matrikkelKommuneService, IMatrikkelStoreClientService matrikkelStoreService, IMatrikkelPersonClientService matrikkelPersonClientService, IOverfoeringServiceClientService overfoeringsClientService,
            IRettsstiftelseClientService rettsstiftelseClientService, IRegisterenhetsrettClientService registerenhetsrettClientService, IInformasjonsServiceClientService informasjonsServiceClientService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger(this.GetType().FullName);
            _identServiceClient = identService;
            _storeServiceClient = storeService;
            _matrikkelenhetServiceClient = matrikkelService;
            _matrikkelKommuneClient = matrikkelKommuneService;
            _matrikkelStoreClient = matrikkelStoreService;
            _matrikkelPersonClient = matrikkelPersonClientService;
            _overfoeringsClientService = overfoeringsClientService;
            _rettsstiftelseClientService = rettsstiftelseClientService;
            _registerenhetsrettClientService = registerenhetsrettClientService;
            _informasjonsServiceClientService = informasjonsServiceClientService;
        }

        public async Task<List<PropertyModel>> FindProperties(string identifier)
        {
            #region oldcrap
            //var heftelser = await _informasjonsServiceClientService.GetPawnStuff(props.id.value);
            //var heftelser2 = await _informasjonsServiceClientService.GetHeftelser(props.id.value);
            //var heftelser3 = await _informasjonsServiceClientService.GetRettsstiftelse("23123");

            //var test = await _rettsstiftelseClientService.GetRettighetForRegisterenhet(props.id.value, props.kommuneId.value);
            //var test = await _registerenhetsrettClientService.GetRetterForEnheter(props.id.value);

            //var test2 = await _storeServiceClient.GetRegisterenhetsrett("3283960");

            //var regrettandel = await _storeServiceClient.GetRettighetsandeler(testprop);

            //var regrett = await _storeServiceClient.GetRegisterenhetsrett(regrettandel.registerenhetsrettId.value);

            //var overfoeringstest = await _overfoeringsClientService.GetOverfoeringerTil(new List<string>() { regrett.id.value });

            //var info = await GetMatrikkelStuffz(props);

            //var testrettsstiftelse = await _storeServiceClient.GetRettsstiftelse("162519726");
            //var testdokument = await _storeServiceClient.GetDokument("107217834");

            //var resusdas = GetFromStoreService<Matrikkelenhet>()
            //Get actual properties
            //var kommuneIdMatrikkel = await _matrikkelKommuneClient.GetKommune(props.kommunenummer);

            //var personidtest = await _matrikkelPersonClient.GetOrganization(identifier);
            //var personTest = await _matrikkelenhetServiceClient.GetMatrikkelenheterForPerson(personidtest);
            #endregion

            var result = new List<PropertyModel>();

            //Get grunnbok identifier for 
            var ident = await _identServiceClient.GetPersonIdentity(identifier);

            //Get all properties owned by identifier
            var propertyids = await GetGrunnBokAndelerForRettighetshavere(ident);

            /* For debug purposes with random test data 
            Random x = new Random(DateTime.Now.Second);
            var temp = x.Next(0, propertyids.Count-1);

            string testprop = propertyids.ElementAt(temp).value; */


            foreach (var propertyid in propertyids)
            {
                string testprop = propertyid.value;

                var regenhetsandelfromstore = await _storeServiceClient.GetRettighetsandeler(testprop);

                var matrikkelenhetgrunnbok = await GetGrunnbokProperties(testprop);

                var heftelserFromRettsstiftelse = await _rettsstiftelseClientService.GetHeftelser(matrikkelenhetgrunnbok.id.value);

                heftelserFromRettsstiftelse = await _storeServiceClient.GetPawnOwnerNames(heftelserFromRettsstiftelse);

                var ownershipTransfer = await _informasjonsServiceClientService.GetOwnershipInfo(matrikkelenhetgrunnbok.id.value);
     

                var kommune = await _storeServiceClient.GetKommune(matrikkelenhetgrunnbok.kommuneId.value);


                var matrikkelenhetid =
                    await _matrikkelenhetServiceClient.GetMatrikkelenhet(matrikkelenhetgrunnbok.gaardsnummer, matrikkelenhetgrunnbok.bruksnummer, matrikkelenhetgrunnbok.festenummer, matrikkelenhetgrunnbok.seksjonsnummer, kommune.Number);

                var matrikkelEnhet = await _matrikkelStoreClient.GetMatrikkelenhet(matrikkelenhetid.value);
                var matrikkelenhetmedteiger = await _matrikkelenhetServiceClient.GetMatrikkelEnhetMedTeiger(matrikkelenhetgrunnbok.gaardsnummer, matrikkelenhetgrunnbok.bruksnummer, matrikkelenhetgrunnbok.festenummer, matrikkelenhetgrunnbok.seksjonsnummer, kommune.Number);

                var teiger = matrikkelenhetmedteiger.bubbleObjects.OfType<Teig>().Select(x=>x.lagretBeregnetAreal).ToList();

                result.Add(new PropertyModel()
                {
                    Grunnbok = new GrunnboksInformasjon()
                    {
                        bnr = matrikkelenhetgrunnbok.bruksnummer.ToString(),
                        gnr = matrikkelenhetgrunnbok.gaardsnummer.ToString(),
                        TeigAreas = teiger,
                        CountyMunicipality = kommune.Name
                    },
                    Documents = heftelserFromRettsstiftelse,
                    HasCulturalHeritageSite = matrikkelEnhet.harKulturminne,
                    Owners = new Rettighetshavere()
                    {
                        EstablishedDate = ownershipTransfer.EstablishedDate, 
                        Share = $"{regenhetsandelfromstore.teller}/{regenhetsandelfromstore.nevner}",
                        Price = $"{ownershipTransfer.Price} {ownershipTransfer.CurrencyCode}" }
                });
            }
            return result;
        }


        private async Task<Matrikkelenhet> GetGrunnbokProperties(string propertyid)
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
                var rregenhet = (Matrikkelenhet) regenhet.@return;

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
