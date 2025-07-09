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
            var matrikkelenhetid = await _matrikkelenhetServiceClient.GetMatrikkelenhet(gaardsNo, bruksNo, festeNo, sectionNo, municipalityNo);
            var bruksenhetid = await _matrikkelBruksenhetService.GetBruksenheter(matrikkelenhetid.value);
            var address = await _matrikkelBruksenhetService.GetAddressForBruksenhet(bruksenhetid.Value);

            return address;
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

            //var matrikkelenhetgrunnbok = await GetGrunnbokProperties(testprop);

            //var matrikkelEnhet = await _matrikkelStoreClient.GetMatrikkelenhet(matrikkelenhetid.value);
            //var matrikkelenhetmedteiger = await _matrikkelenhetServiceClient.GetMatrikkelEnhetMedTeiger(matrikkelenhetgrunnbok.gaardsnummer, matrikkelenhetgrunnbok.bruksnummer, matrikkelenhetgrunnbok.festenummer, matrikkelenhetgrunnbok.seksjonsnummer, kommune.Number);

            //var matrikkelEnhet = matrikkelenhetmedteiger.bubbleObjects.OfType<Grunneiendom>().FirstOrDefault();
            //var teiger = matrikkelenhetmedteiger.bubbleObjects.OfType<Teig>().Select(x=>x.lagretBeregnetAreal).ToList();
            #endregion

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
