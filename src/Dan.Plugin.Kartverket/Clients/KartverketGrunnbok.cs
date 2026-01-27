using Dan.Common.Exceptions;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Clients.Matrikkel;
using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using Kartverket.Matrikkel.StoreService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients
{
    public interface IKartverketGrunnbokMatrikkelService
    {
        //public Task<KartverketResponse> Get(string ssn);
        public Task<List<PropertyModel>> FindProperties(string identifier);

        public Task<List<PropertyEkstra>> FindOwnedProperties(string identifier);
        public Task<KartverketResponse> GetAddresses(KartverketResponse kartverketResponse, bool singleAddress = false);
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
        private IMatrikkelAdresseClientService _matrikkelAdresseClientService;

        public KartverketGrunnbokMatrikkelService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IIdentServiceClientService identService, IStoreServiceClientService storeService, IMatrikkelenhetClientService matrikkelService,
            IMatrikkelKommuneClientService matrikkelKommuneService, IMatrikkelStoreClientService matrikkelStoreService, IMatrikkelPersonClientService matrikkelPersonClientService, IOverfoeringServiceClientService overfoeringsClientService,
            IRettsstiftelseClientService rettsstiftelseClientService, IRegisterenhetsrettClientService registerenhetsrettClientService, IInformasjonsServiceClientService informasjonsServiceClientService, IRegisterenhetsRettsandelsServiceClientService regRettsandelsClientService,
            IMatrikkelBygningClientService matrikkelbygningClientService, IMatrikkelBruksenhetService matrikkelBruksenhetService, IMatrikkelAdresseClientService matrikkelAdresseClientService)
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
            _matrikkelAdresseClientService = matrikkelAdresseClientService;
        }

        public async Task<KartverketResponse> GetAddresses(KartverketResponse input, bool singleAddress = false)
        {
            List<Property> properties = new List<Property>();
            var taskList = new List<Task>();

            foreach (Property property in input.PropertyRights.Properties)
            {
                properties.Add(property);
                taskList.Add(UpdatePropertyAddresses(property, singleAddress));
            }

            input.PropertyRights.Properties = properties;

            List<PropertyWithRights> propertyWithRights = new List<PropertyWithRights>();
            foreach (PropertyWithRights property in input.PropertyRights.PropertiesWithRights)
            {
                propertyWithRights.Add(property);
                taskList.Add(UpdatePropertyAddresses(property, singleAddress));
            }

            input.PropertyRights.PropertiesWithRights = propertyWithRights;

            await Task.WhenAll(taskList).ConfigureAwait(false);

            return input;
        }

        private async Task UpdatePropertyAddresses(Property property, bool singleAddress = false)
        {
            int gnr, bnr, fnr, snr = 0;
            long knr = 0;

            //parse property numbers as kartverket expects integers
            Int32.TryParse(property.HoldingNumber, out gnr);
            Int32.TryParse(property.SubholdingNumber, out bnr);
            Int32.TryParse(property.LeaseNumber, out fnr);
            Int32.TryParse(property.SectionNumber, out snr);
            long.TryParse(property.MunicipalityNumber, out knr);

            //resulting list of addresses for the property
            var addressList = new List<string>();

            //Andels flats have already had their information set
            if (!string.IsNullOrEmpty(property.Address) && !string.IsNullOrEmpty(property.PostalCode))
            {
                return;
            }

            if (gnr == 0 || knr == 0)
                return;

            try
            {
                //get matrikkelenhetid and then the actual matrikkelenhet
                var matrikkelenhetid = await _matrikkelenhetServiceClient.GetMatrikkelenhet(gnr, bnr, fnr, snr, property.MunicipalityNumber);

                var kommune = await _matrikkelStoreClient.GetKommune(knr);

                if (string.IsNullOrEmpty(kommune.kommunenavn))
                    throw new EvidenceSourcePermanentClientException(1010, "Invalid municipality number provided");

                property.Municipality = kommune.kommunenavn;

                //Matrikkelenhet does not exist so we can just abort the process
                if (matrikkelenhetid?.value == null || matrikkelenhetid.value == 0)
                    return;

                var bruksenhetider = await _matrikkelBruksenhetService.GetBruksenheter(matrikkelenhetid.value);

                foreach (var id in bruksenhetider)
                {
                    if (singleAddress && addressList.Count > 1)
                        break;

                    var bruksenhet = await _matrikkelStoreClient.GetBruksenhet(id.value);

                    if (bruksenhet?.adresseId?.value == null)
                        continue;

                    var adresse = await _matrikkelStoreClient.GetAdresse(bruksenhet.adresseId.value);
                    bool addressFound = false, postalCodeFound = false;

                    if (adresse is Vegadresse roadAddress)
                    {
                        var veg = await _matrikkelStoreClient.GetVeg(roadAddress.vegId.value);
                        var tmpAddress = veg.adressenavn + " " + roadAddress.nummer + roadAddress.bokstav;

                        if (!string.IsNullOrEmpty(tmpAddress))
                        {
                            if (!addressList.Contains(tmpAddress))
                            {
                                addressList.Add(tmpAddress);
                                addressFound = true;
                            }
                        }

                        if (string.IsNullOrEmpty(property.PostalCode) || string.IsNullOrEmpty(property.City))
                        {
                            (property.PostalCode, property.City) = await GetPostalInformation(adresse.kretsIds);
                            postalCodeFound = true;
                        }
                        else
                        {
                            postalCodeFound = true;
                        }
                    }

                    if (!postalCodeFound && !addressFound)
                    {
                        var result = await _matrikkelBruksenhetService.GetAddressForBruksenhet(id.value);

                        if (!string.IsNullOrEmpty(result))
                        {
                            if (!addressList.Contains(result))
                            {
                                addressList.Add(result);
                            }
                        }
                    }
                    addressFound = false;
                    postalCodeFound = false;
                }

                var matrikkelUnitAddressList = await _matrikkelAdresseClientService.GetAdresserForMatrikkelenhet(matrikkelenhetid.value);

                foreach (var address in matrikkelUnitAddressList.Reverse())
                {
                    if (singleAddress && addressList.Count > 1)
                        break;

                    var matrikkelAdress = await _matrikkelStoreClient.GetAdresse(address.value);

                    if (matrikkelAdress is Vegadresse roadAddress)
                    {
                        var vegId = ((Vegadresse)matrikkelAdress).vegId.value;
                        var veg = await _matrikkelStoreClient.GetVeg(vegId);

                        var tmpadress = veg.adressenavn + " " + roadAddress.nummer + roadAddress.bokstav;
                        if (!string.IsNullOrEmpty(tmpadress))
                        {
                            if (!addressList.Contains(tmpadress))
                            {
                                addressList.Add(tmpadress);
                            }
                        }

                        if (string.IsNullOrEmpty(property.PostalCode) || string.IsNullOrEmpty(property.City))
                        {
                            (property.PostalCode, property.City) = await GetPostalInformation(matrikkelAdress.kretsIds);
                        }
                    }
                }
                _logger.LogInformation($"Found {addressList.Count} addresses for property gnr = {gnr}, bnr = {bnr}, fnr = {fnr}, snr = {snr}, knr = {property.MunicipalityNumber}");

                //if consumer only wants a single address, return the first one found and avoid concatinating multiple addresses
                if (singleAddress)
                {
                    if (addressList.Count > 0)
                    {
                        property.AddressList.Add(addressList.First());
                        property.Address = addressList.First();
                        property.HasMoreAddresses = (addressList.Count > 1);
                    }
                    else
                    {
                        _logger.LogWarning($"No addresses found for property gnr = {gnr}, bnr = {bnr}, fnr = {fnr}, snr = {snr}, knr = {property.MunicipalityNumber}");
                    }
                }
                else
                {
                    property.Address = string.Join(", ", addressList);
                    property.AddressList = addressList;
                    property.HasMoreAddresses = false;
                }
            }
            catch (EvidenceSourcePermanentClientException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kartverket::OED::Error getting addresses for property gnr = {gnr}, bnr = {bnr}, fnr = {fnr}, snr = {snr}, knr = {knr}", gnr, bnr, fnr, snr, knr);
            }
        }

        private async Task<(string code, string city)> GetPostalInformation(KretsId[] kretsList)
        {
            foreach (var kretsid in kretsList)
            {
                var krets = await _matrikkelStoreClient.GetKrets(kretsid.value);

                if (krets is Postnummeromrade)
                {
                    return (((Postnummeromrade)krets).kretsnummer.ToString(), ((Postnummeromrade)krets).kretsnavn);
                }
            }
            return ("", "");
        }

        public async Task<string> GetAddressForSection(int gaardsNo, int bruksNo, int festeNo, string municipalityNo, int sectionNo)
        {
            try
            {
                var matrikkelenhetid = await _matrikkelenhetServiceClient.GetMatrikkelenhet(gaardsNo, bruksNo, festeNo, sectionNo, municipalityNo);
                var test = await _matrikkelStoreClient.GetMatrikkelenhet(matrikkelenhetid.value);
                var bruksenhetider = await _matrikkelBruksenhetService.GetBruksenheter(matrikkelenhetid.value);
                var result = string.Empty;

                foreach (var id in bruksenhetider)
                {
                    result += "/n" + await _matrikkelBruksenhetService.GetAddressForBruksenhet(id.value);
                }

                return result;
            }
            catch (Exception ex)
            {
                //just return empty string - if getaddressforbruksenhet cannot find an address, the matrikkel service also returns an empty string
                _logger.LogError(ex, "Kartverket::OED::Error getting address for section {gaardsNo}/{bruksNo}/{festeNo}/{municipalityNo}/{sectionNo}", gaardsNo, bruksNo, festeNo, municipalityNo, sectionNo);
                return "";
            }
        }

        public async Task<List<PropertyEkstra>> FindOwnedProperties(string identifier)
        {
            var result = new List<PropertyEkstra>();

            //Get grunnbok identifier for
            var ident = await _identServiceClient.GetPersonIdentity(identifier);

            //Get all properties owned by identifier
            var regrettsandelListe = await _regRettsandelsClientService.GetAndelerForRettighetshaver(ident);
            foreach (var registerenhetsrettsandelid in regrettsandelListe)
            {
                var regenhetsandelfromstore = await _storeServiceClient.GetRettighetsandeler(registerenhetsrettsandelid);
                var matrikkelenhetgrunnbok = await _storeServiceClient.GetMatrikkelEnhetFromRegisterRettighetsandel(regenhetsandelfromstore.registerenhetsrettId.value);

                var kommune = new Dan.Plugin.Kartverket.Models.Kommune();
                if (matrikkelenhetgrunnbok != null)
                {
                    kommune = await _storeServiceClient.GetKommune(matrikkelenhetgrunnbok.kommuneId.value);
                    result.Add(new PropertyEkstra()
                    {
                        Grunnbok = new EkstraGrunnbokdata()
                        {
                            Kommunenummer = kommune.Number ?? null,
                            CountyMunicipality = kommune.Name ?? null,
                            Bruksnummer = matrikkelenhetgrunnbok.bruksnummer.ToString(),
                            Gardsnummer = matrikkelenhetgrunnbok.gaardsnummer.ToString(),
                            Festenummer = matrikkelenhetgrunnbok.festenummer.ToString(),
                            Seksjonsnummer = matrikkelenhetgrunnbok.seksjonsnummer.ToString()
                        },
                        Owners = new Rettighetshavere()
                        {
                            Share = $"{regenhetsandelfromstore.teller}/{regenhetsandelfromstore.nevner}"
                        }
                    });
                }
                continue;
            }
            return result;
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
