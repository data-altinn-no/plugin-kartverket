using System;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Clients.Matrikkel;
using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kartverket.Grunnbok.IdentService;
using Kartverket.Grunnbok.StoreService;

namespace Dan.Plugin.Kartverket.Clients
{
    public interface IKartverketGrunnbokMatrikkelService
    {
        //public Task<KartverketResponse> Get(string ssn);
        public Task<List<PropertyModel>> FindProperties(string identifier);
        public Task<string> GetAddressForSection(int gaardsNo, int bruksNo, int festeNo, string municipalityNo, int sectionNo);
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
        private IRegisterenhetsRettsandelsServiceClientService _regRettsandelsClientService;
        private IMatrikkelBygningClientService _matrikkelbygningClientService;
        private IMatrikkelBruksenhetService _matrikkelBruksenhetService;

        public KartverketGrunnbokMatrikkelService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IIdentServiceClientService identService, IStoreServiceClientService storeService, IMatrikkelenhetClientService matrikkelService,
            IMatrikkelKommuneClientService matrikkelKommuneService, IMatrikkelStoreClientService matrikkelStoreService, IMatrikkelPersonClientService matrikkelPersonClientService, IOverfoeringServiceClientService overfoeringsClientService,
            IRettsstiftelseClientService rettsstiftelseClientService, IRegisterenhetsrettClientService registerenhetsrettClientService, IInformasjonsServiceClientService informasjonsServiceClientService, IRegisterenhetsRettsandelsServiceClientService regRettsandelsClientService,
            IMatrikkelBygningClientService matrikkelbygningClientService, IMatrikkelBruksenhetService matrikkelBruksenhetService)
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
            _regRettsandelsClientService = regRettsandelsClientService;
            _matrikkelbygningClientService = matrikkelbygningClientService;
            _matrikkelBruksenhetService = matrikkelBruksenhetService;
        }

        public async Task<string> GetAddressForSection(int gaardsNo, int bruksNo, int festeNo, string municipalityNo, int sectionNo)
        {            
            try
            {
                var matrikkelenhetid = await _matrikkelenhetServiceClient.GetMatrikkelenhet(gaardsNo, bruksNo, festeNo, sectionNo, municipalityNo);
                var test = await _matrikkelStoreClient.GetMatrikkelenhet(matrikkelenhetid.value);
                var bruksenhetid = await _matrikkelBruksenhetService.GetBruksenheter(matrikkelenhetid.value);

                /*
                if (bruksenhetid == null)
                {
                    var test2 = await _matrikkelStoreClient.GetMatrikkelenhetSeksjon(matrikkelenhetid.value);
                    var test3 = await _matrikkelStoreClient.GetMatrikkelenhet(test2.seksjonertMatrikkelenhetIds.FirstOrDefault().value);
                    var test4 = await _matrikkelBruksenhetService.GetBruksenheter(test2.seksjonertMatrikkelenhetIds.FirstOrDefault().value);
                    var test5 = await _matrikkelBruksenhetService.GetAddressForBruksenhet(test4.Value);
                    return test5;
                } */
                 return await _matrikkelBruksenhetService.GetAddressForBruksenhet(bruksenhetid.Value);             
            }
            catch (Exception ex)
            {
                //just return empty string - if getaddressforbruksenhet cannot find an address, the matrikkel service also returns an empty string
                _logger.LogError(ex, "Kartverket::OED::Error getting address for section {gaardsNo}/{bruksNo}/{festeNo}/{municipalityNo}/{sectionNo}", gaardsNo, bruksNo, festeNo, municipalityNo, sectionNo);
                return "";
            }
        }

        public async Task<List<PropertyModel>> FindProperties(string identifier)
        {
            var result = new List<PropertyModel>();

            //Get grunnbok identifier for
            var ident = await _identServiceClient.GetPersonIdentity(identifier);

            //Get all properties owned by identifier
            var regrettsandelListe = await _regRettsandelsClientService.GetAndelerForRettighetshaver(ident);

            var numbersToRemove = Math.Min(10, regrettsandelListe.Count);
            regrettsandelListe.RemoveRange(numbersToRemove, regrettsandelListe.Count - numbersToRemove);

            foreach (var registerenhetsrettsandelid in regrettsandelListe)
            {
                var regenhetsandelfromstore = await _storeServiceClient.GetRettighetsandeler(registerenhetsrettsandelid);

                var matrikkelenhetgrunnbok = await _storeServiceClient.GetMatrikkelEnhetFromRegisterRettighetsandel(regenhetsandelfromstore.registerenhetsrettId.value);

                var heftelserFromRettsstiftelse = await _rettsstiftelseClientService.GetHeftelser(matrikkelenhetgrunnbok.id.value);

                heftelserFromRettsstiftelse = await _storeServiceClient.GetPawnOwnerNames(heftelserFromRettsstiftelse);

                var ownershipTransfer = await _informasjonsServiceClientService.GetOwnershipInfo(matrikkelenhetgrunnbok.id.value);

                var kommune = await _storeServiceClient.GetKommune(matrikkelenhetgrunnbok.kommuneId.value);

                var matrikkelenhetid =
                    await _matrikkelenhetServiceClient.GetMatrikkelenhet(matrikkelenhetgrunnbok.gaardsnummer, matrikkelenhetgrunnbok.bruksnummer, matrikkelenhetgrunnbok.festenummer, matrikkelenhetgrunnbok.seksjonsnummer, kommune.Number);

                var bygningsider = await _matrikkelbygningClientService.GetBygningerForMatrikkelenhet(matrikkelenhetid.value);

                var buildings = new List<double>();
                foreach (var id in bygningsider)
                {
                    var temp = await _matrikkelStoreClient.GetBygning(id);
                    buildings.Add(temp == null ? 0 : temp.bebygdAreal);
                }

                var matrikkelEnhet = await _matrikkelenhetServiceClient.GetMatrikkelEnhetTeig(matrikkelenhetgrunnbok.gaardsnummer, matrikkelenhetgrunnbok.bruksnummer,
                    matrikkelenhetgrunnbok.festenummer, matrikkelenhetgrunnbok.seksjonsnummer, kommune.Number);

                result.Add(new PropertyModel()
                {
                    Grunnbok = new GrunnboksInformasjon()
                    {
                        bnr = matrikkelenhetgrunnbok.bruksnummer.ToString(),
                        gnr = matrikkelenhetgrunnbok.gaardsnummer.ToString(),
                        TeigAreas = matrikkelEnhet.Teiger,
                        CountyMunicipality = kommune.Name,
                        BuildingArea = buildings.Sum()
                    },
                    Documents = heftelserFromRettsstiftelse,
                    HasCulturalHeritageSite = matrikkelEnhet.HasCulturalHeritageSite,
                    Owners = new Rettighetshavere()
                    {
                        EstablishedDate = (ownershipTransfer == null) ? null : ownershipTransfer.EstablishedDate,
                        Share = $"{regenhetsandelfromstore.teller}/{regenhetsandelfromstore.nevner}",
                        Price = (ownershipTransfer == null) ? "" : $"{ownershipTransfer.Price} {ownershipTransfer.CurrencyCode}" }
                });
            }
            return result;
        }
    }
}
