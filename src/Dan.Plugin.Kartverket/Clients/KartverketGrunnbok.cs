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
using System.Linq;
using System.Threading.Tasks;
using MatrikkelenhetId = Kartverket.Matrikkel.MatrikkelenhetService.MatrikkelenhetId;
using BruksenhetServiceBruksenhetId = Kartverket.Matrikkel.BruksenhetService.BruksenhetId;
using AdresseServiceAdresseId = Kartverket.Matrikkel.AdresseService.AdresseId;

namespace Dan.Plugin.Kartverket.Clients
{
    public interface IKartverketGrunnbokMatrikkelService
    {
        public Task<List<PropertyModel>> FindProperties(string identifier);

        public Task<List<PropertyWithOwners>> FindOwnedProperties(string identifier);
        public Task<KartverketResponse> GetAddresses(KartverketResponse kartverketResponse, bool singleAddress = false);
        public Task<string> GetAddressForSection(int gaardsNo, int bruksNo, int festeNo, string municipalityNo, int sectionNo);
        public Task<bool> PropertyHasFritidsbolig(string matrikkelNumber);
        Task<List<Address>> GetAdresseByMatrikkelNumber(string matrikkelNumber);
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
            var (gnr, bnr, fnr, snr, knr) = ParseMatrikkelKey(property);

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

                var bruksenhetListTask = _matrikkelBruksenhetService.GetBruksenheter(matrikkelenhetid.value);
                var matrikkelAddrListTask = _matrikkelAdresseClientService.GetAdresserForMatrikkelenhet(matrikkelenhetid.value);
                await Task.WhenAll(bruksenhetListTask, matrikkelAddrListTask).ConfigureAwait(false);

                // Fan-out: alle bruksenheter + alle matrikkelenhet-adresser samtidig
                var bruksenhetTasks = bruksenhetListTask.Result.Select(id => FetchFromBruksenhetAsync(id));
                var matrikkelTasks = matrikkelAddrListTask.Result.Reverse().Select(addr => FetchFromMatrikkelAddressAsync(addr));

                var allResults = await Task.WhenAll(bruksenhetTasks.Concat(matrikkelTasks)).ConfigureAwait(false);

                // Fan-in: deterministisk fletting, beholder dagens "første-vinner"-semantikk for postnummer
                foreach (var result in allResults)
                {
                    if (result is null) continue;
                    if (singleAddress && addressList.Count > 1) break;

                    var tuple = result.Value;

                    if (!string.IsNullOrEmpty(tuple.street) && !addressList.Contains(tuple.street))
                        addressList.Add(tuple.street);

                    if ((string.IsNullOrEmpty(property.PostalCode) || string.IsNullOrEmpty(property.City))
                        && !string.IsNullOrEmpty(tuple.postal))
                    {
                        property.PostalCode = tuple.postal;
                        property.City = tuple.city;
                    }
                }

                _logger.LogInformation($"Found {addressList.Count} addresses for property gnr = {gnr}, bnr = {bnr}, fnr = {fnr}, snr = {snr}, knr = {property.MunicipalityNumber}");

                //if consumer only wants a single address, return the first one found and avoid concatinating multiple addresses
                if (singleAddress)
                {
                    if (addressList.Count > 0)
                    {
                        property.AddressList.Add(addressList[0]);
                        property.Address = addressList[0];
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

        private static (int gnr, int bnr, int fnr, int snr, long knr) ParseMatrikkelKey(Property property)
        {
            int.TryParse(property.HoldingNumber, out var gnr);
            int.TryParse(property.SubholdingNumber, out var bnr);
            int.TryParse(property.LeaseNumber, out var fnr);
            int.TryParse(property.SectionNumber, out var snr);
            long.TryParse(property.MunicipalityNumber, out var knr);
            return (gnr, bnr, fnr, snr, knr);
        }

        private async Task<string> BuildVegadresseString(Vegadresse roadAddress)
        {
            var veg = await _matrikkelStoreClient.GetVeg(roadAddress.vegId.value);
            if (veg == null)
                return string.Empty;
            return veg.adressenavn + " " + roadAddress.nummer + roadAddress.bokstav;
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
                var bruksenhetider = await _matrikkelBruksenhetService.GetBruksenheter(matrikkelenhetid.value);

                var addresses = new List<string>();

                foreach (var id in bruksenhetider)
                {
                    var address = await _matrikkelBruksenhetService.GetAddressForBruksenhet(id.value);

                    if (!string.IsNullOrWhiteSpace(address))
                        addresses.Add(address);
                }

                return string.Join("\n", addresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kartverket::OED::Error getting address for section {gaardsNo}/{bruksNo}/{festeNo}/{municipalityNo}/{sectionNo}", gaardsNo, bruksNo, festeNo, municipalityNo, sectionNo);
                return "";
            }
        }

        public async Task<List<PropertyWithOwners>> FindOwnedProperties(string identifier)
        {
            var result = new List<PropertyWithOwners>();

            var ident = await _identServiceClient.GetPersonIdentity(identifier);

            var registerRettsAndelList = await _regRettsandelsClientService.GetAndelerForRettighetshaver(ident);
            foreach (var registerenhetsrettsandelid in registerRettsAndelList)
            {
                var regenhetsandelfromstore = await _storeServiceClient.GetRettighetsandeler(registerenhetsrettsandelid);

                //Skip if property is historical, as we only want currently owned properties.
                if (regenhetsandelfromstore.historisk)
                    continue;
                try
                {
                    var matrikkelenhetgrunnbok = await _storeServiceClient.GetMatrikkelEnhetFromRegisterRettighetsandel(regenhetsandelfromstore.registerenhetsrettId.value);

                    var listOfCoOwners = new List<CoOwner>();

                    var share = $"{regenhetsandelfromstore.teller}/{regenhetsandelfromstore.nevner}";
                    if (regenhetsandelfromstore.teller != regenhetsandelfromstore.nevner && matrikkelenhetgrunnbok != null)
                    {
                        var registerenhetId = matrikkelenhetgrunnbok.id.value;

                        var registerEnhetTilRegisterenhetsrettId = await _registerenhetsrettClientService.GetRetterForEnheter(registerenhetId);

                        var registerEnhetIdTilRegisterenhetsrettIds = registerEnhetTilRegisterenhetsrettId.Values
                            .SelectMany(rettid => rettid.Select(ids => ids.value))
                            .ToList();

                        foreach (var registerEnhetId in registerEnhetIdTilRegisterenhetsrettIds)
                        {
                            var andelerIRetter = await _regRettsandelsClientService.GetAndelerIRetter(regenhetsandelfromstore.registerenhetsrettId.value);
                            var andelerIRetterValues = andelerIRetter.Body.@return.Values;

                            var firstAndel = andelerIRetterValues.FirstOrDefault();
                            if (firstAndel == null)
                                continue;

                            foreach (var andel in firstAndel)
                            {
                                var andeler = await _storeServiceClient.GetRettighetsandeler(andel.value.ToString());

                                if (!andeler.historisk)
                                {
                                    var coOwner = await _storeServiceClient.GetPerson(andeler.rettighetshaverId.value);
                                    if (coOwner == null)
                                        continue;

                                    listOfCoOwners.Add(new CoOwner()
                                    {
                                        Identifier = coOwner.identifikasjonsnummer ?? null,
                                        Name = coOwner.navn ?? null,
                                        OwnerShare = $"{andeler.teller}/{andeler.nevner}" ?? null
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        //for single owners
                        var owner = await _storeServiceClient.GetPerson(regenhetsandelfromstore.rettighetshaverId.value);
                        listOfCoOwners.Add(new CoOwner
                        {
                            Identifier = owner.identifikasjonsnummer ?? null,
                            Name = owner.navn ?? null,
                            OwnerShare = $"{regenhetsandelfromstore.teller}/{regenhetsandelfromstore.nevner}" ?? null
                        });
                    }

                    var kommune = new Models.Kommune();
                    if (matrikkelenhetgrunnbok != null)
                        kommune = await _storeServiceClient.GetKommune(matrikkelenhetgrunnbok.kommuneId.value);

                    var addresseList = new List<Address>();
                    var boligType = new List<string>();

                    var matrikkelenhetid = await _matrikkelenhetServiceClient.GetMatrikkelenhet(matrikkelenhetgrunnbok.gaardsnummer, matrikkelenhetgrunnbok.bruksnummer, matrikkelenhetgrunnbok.festenummer, matrikkelenhetgrunnbok.seksjonsnummer, kommune.Number);
                    var bruksenhetIds = await _matrikkelBruksenhetService.GetBruksenheter(matrikkelenhetid.value);
                    if(bruksenhetIds.Any())
                    {
                        foreach(var bruksenhetId in bruksenhetIds)
                        {
                            //check if property is fritidsbolig
                            var bruksenhet = await _matrikkelStoreClient.GetBruksenhet(bruksenhetId.value);
                            if(bruksenhet.bruksenhetstypeKodeId != null)
                            {
                                var bruksenhetstype = await _matrikkelStoreClient.GetBruksenhetstype(bruksenhet.bruksenhetstypeKodeId.value);
                                boligType.Add(bruksenhetstype.kodeverdi);
                            }                                

                            //get address
                            if(bruksenhet.adresseId != null)
                            {
                                var adresseByBruksenhet = await GetAddresseByBruksenhet(bruksenhet, matrikkelenhetid.value);
                                if (adresseByBruksenhet != null)
                                    addresseList.Add(adresseByBruksenhet);
                            }
                                
                        }

                        //we need to get the address one more time in case the bruksenhet didn't have a addresseId
                        var adresseByMatrikkelenhetId = await GetAddressByMatrikkelenhetId(matrikkelenhetid.value);
                        addresseList.Add(adresseByMatrikkelenhetId); 
                    }

                    //remove duplicates and empty addresses, some properties have multiple bruksenheter and matrikkelenhet linked to the same address which causes duplicates in the list
                    addresseList = addresseList
                        .Where(a => !string.IsNullOrWhiteSpace(a.Street) &&
                                    !string.IsNullOrWhiteSpace(a.PostalCode) &&
                                    !string.IsNullOrWhiteSpace(a.City))
                        .GroupBy(a => new
                        {
                            Street = a.Street?.Trim().ToLower(),
                            PostalCode = a.PostalCode?.Trim(),
                            City = a.City?.Trim().ToLower()
                        })
                        .Select(g => g.First())
                        .ToList();

                    result.Add(new PropertyWithOwners()
                    {
                        PropertyData = new PropertyData()
                        {
                            Kommunenummer = kommune.Number ?? null,
                            Kommunenavn = kommune.Name ?? null,
                            Bruksnummer = matrikkelenhetgrunnbok?.bruksnummer.ToString() ?? null,
                            Gardsnummer = matrikkelenhetgrunnbok?.gaardsnummer.ToString() ?? null,
                            Festenummer = matrikkelenhetgrunnbok?.festenummer.ToString() ?? null,
                            Seksjonsnummer = matrikkelenhetgrunnbok?.seksjonsnummer.ToString() ?? null
                        },
                        Owners = listOfCoOwners,
                        Addresses = addresseList,
                        IsFritidsbolig = boligType.Contains("F")
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error when calling FindOwnedProperties");
                }
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

        public async Task<bool> PropertyHasFritidsbolig(string matrikkelNumber)
        {
            var matrikkelenhetid = await GetMatrikkelenhetByMatrikelNumber(matrikkelNumber);
            if(matrikkelenhetid == null || matrikkelenhetid.value == 0)
            {                
                return false;
            }

            var bruksenhetIder = await _matrikkelBruksenhetService.GetBruksenheter(matrikkelenhetid.value);

            foreach(var bruksenhetId in bruksenhetIder)
            {
                var bruksenhet = await _matrikkelStoreClient.GetBruksenhet(bruksenhetId.value);
                if (bruksenhet.bruksenhetstypeKodeId != null)
                {
                    var bruksenhetstype = await _matrikkelStoreClient.GetBruksenhetstype(bruksenhet.bruksenhetstypeKodeId.value);
                    if (bruksenhetstype.kodeverdi.Equals("F"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public async Task<List<Address>> GetAdresseByMatrikkelNumber(string matrikkelNumber)
        {
            var matrikkelenhetid = await GetMatrikkelenhetByMatrikelNumber(matrikkelNumber);
            if(matrikkelenhetid == null || matrikkelenhetid.value == 0)
            {
                _logger.LogWarning($"No matrikkelenhet found for matrikkel number {matrikkelNumber}");
                return new List<Address>();
            }

            var adresseList = new List<Address>();

            var adresse = await GetAddressByMatrikkelenhetId(matrikkelenhetid.value);
            if(adresse != null)
            {
                adresseList.Add(adresse);
            }

            var bruksenhetIder = await _matrikkelBruksenhetService.GetBruksenheter(matrikkelenhetid.value);
            foreach (var bruksenhetId in bruksenhetIder)
            {
                var bruksenhet = await _matrikkelStoreClient.GetBruksenhet(bruksenhetId.value);
                if (bruksenhet.adresseId != null)
                {
                    var adresseByBruksenhet = await GetAddresseByBruksenhet(bruksenhet, matrikkelenhetid.value);
                    if (adresseByBruksenhet != null)
                        adresseList.Add(adresseByBruksenhet);
                }
            }
            
            //remove duplicates and empty addresses, some properties have multiple bruksenheter and matrikkelenhet linked to the same address which causes duplicates in the list
            adresseList = adresseList
                .Where(a => !string.IsNullOrWhiteSpace(a.Street) &&
                            !string.IsNullOrWhiteSpace(a.PostalCode) &&
                            !string.IsNullOrWhiteSpace(a.City))
                .GroupBy(a => new
                {
                    Street = a.Street?.Trim().ToLower(),
                    PostalCode = a.PostalCode?.Trim(),
                    City = a.City?.Trim().ToLower()
                })
                .Select(g => g.First())
                .ToList();

            return adresseList;
        }

        private async Task<Address> GetAddressByMatrikkelenhetId(long matrikkelenhetId)
        {
            var theAddress = new Address();

            var matrikkelEnhetAddresseListe = await _matrikkelAdresseClientService.GetAdresserForMatrikkelenhet(matrikkelenhetId);
            foreach(var adresse in matrikkelEnhetAddresseListe)
            {
                var matrikkelAdress = await _matrikkelStoreClient.GetAdresse(adresse.value);

                if (matrikkelAdress is Vegadresse roadAddress)
                {
                    var vegId = ((Vegadresse)matrikkelAdress).vegId.value;
                    var veg = await _matrikkelStoreClient.GetVeg(vegId);

                    var tmpadress = veg.adressenavn + " " + roadAddress.nummer + roadAddress.bokstav;
                    if (!string.IsNullOrEmpty(tmpadress))
                    {
                        theAddress.Street = tmpadress;
                    }

                    (var postalCode, var city) = await GetPostalInformation(matrikkelAdress.kretsIds);
                    if (!string.IsNullOrWhiteSpace(postalCode) && !string.IsNullOrWhiteSpace(city))
                    {
                        theAddress.PostalCode = postalCode;
                        theAddress.City = city;
                    }
                }
                else
                {
                    //if the address it not VegAdreesse we just get the postalnumber and city to use later
                    (var postalCode, var city) = await GetPostalInformation(matrikkelAdress.kretsIds);
                    if (!string.IsNullOrWhiteSpace(postalCode) && !string.IsNullOrWhiteSpace(city))
                    {
                        theAddress.PostalCode = postalCode;
                        theAddress.City = city;
                    }
                }
            }
            return theAddress;
        }

        private async Task<Address> GetAddresseByBruksenhet(Bruksenhet bruksenhet, long matrikkelenhetId)
        {
            Address theAddress = new Address();

            var adresse = await _matrikkelStoreClient.GetAdresse(bruksenhet.adresseId.value);
            if (adresse is Vegadresse roadAddress)
            {
                var veg = await _matrikkelStoreClient.GetVeg(roadAddress.vegId.value);
                var tmpAddress = veg.adressenavn + " " + roadAddress.nummer + roadAddress.bokstav;

                (var postalCode, var city) = await GetPostalInformation(adresse.kretsIds);
                if (!string.IsNullOrWhiteSpace(postalCode) && !string.IsNullOrWhiteSpace(city))
                {
                    theAddress.Street = tmpAddress;
                    theAddress.PostalCode = postalCode;
                    theAddress.City = city;
                }
            }
            else
            {
                //if no address if found because it is not a vegadresse, try to get the address by the bruksenhetid, this is a fallback as some properties have addresses linked to the bruksenhet and not the matrikkelenhet
                var addresse = await _matrikkelBruksenhetService.GetAddressForBruksenhet(bruksenhet.id.value);

                if (!string.IsNullOrWhiteSpace(addresse))
                    theAddress.Street = addresse;

                var matrikkelEnhetAddresseListe = await _matrikkelAdresseClientService.GetAdresserForMatrikkelenhet(matrikkelenhetId);
                foreach (var add in matrikkelEnhetAddresseListe)
                {
                    var matrikkelAdress = await _matrikkelStoreClient.GetAdresse(add.value);
                    //if the address it not VegAdreesse we just get the postalnumber and city to use later
                    (var postalCode, var city) = await GetPostalInformation(matrikkelAdress.kretsIds);
                    if (!string.IsNullOrWhiteSpace(postalCode) && !string.IsNullOrWhiteSpace(city))
                    {
                        theAddress.PostalCode = postalCode;
                        theAddress.City = city;
                    }
                }
            }

            return theAddress;
        }

        private async Task<MatrikkelenhetId> GetMatrikkelenhetByMatrikelNumber(string matrikkelNumber)
        {
            if(string.IsNullOrWhiteSpace(matrikkelNumber))
                throw new ArgumentException("Matrikkel number cannot be null or empty", nameof(matrikkelNumber));           

            var matrikkelnummerSplit = matrikkelNumber.Split('-', '/');

            if(matrikkelnummerSplit.Length == 0)
                throw new ArgumentException("Matrikkel number must contain at least a municipality number and a gnr", nameof(matrikkelNumber));

            var kommunenr = matrikkelnummerSplit[0]?.Trim();
            if(string.IsNullOrWhiteSpace(kommunenr))
                throw new ArgumentException("Matrikkel number must contain a valid municipality number", nameof(matrikkelNumber));

            var gnr = matrikkelnummerSplit.Length > 1 && Int32.TryParse(matrikkelnummerSplit[1], out var gnrVal) ? gnrVal : 0;
            var bnr = matrikkelnummerSplit.Length > 2 && Int32.TryParse(matrikkelnummerSplit[2], out var bnrVal) ? bnrVal : 0;
            var fnr = matrikkelnummerSplit.Length > 3 && Int32.TryParse(matrikkelnummerSplit[3], out var fnrVal) ? fnrVal : 0;
            var snr = matrikkelnummerSplit.Length > 4 && Int32.TryParse(matrikkelnummerSplit[4], out var snrVal) ? snrVal : 0;

            return await _matrikkelenhetServiceClient.GetMatrikkelenhet(gnr, bnr, fnr, snr, kommunenr);
        }
        
        private async Task<(string street, string postal, string city)?> FetchFromBruksenhetAsync(BruksenhetServiceBruksenhetId id)
        {
            var bruksenhet = await _matrikkelStoreClient.GetBruksenhet(id.value).ConfigureAwait(false);
            if (bruksenhet?.adresseId?.value == null)
                return null;

            var adresse = await _matrikkelStoreClient.GetAdresse(bruksenhet.adresseId.value).ConfigureAwait(false);

            if (adresse is Vegadresse roadAddress)
            {
                // De to kallene er uavhengige — kjør dem parallelt:
                var streetTask = BuildVegadresseString(roadAddress);
                var postalTask = GetPostalInformation(adresse.kretsIds);
                await Task.WhenAll(streetTask, postalTask).ConfigureAwait(false);

                var postal = postalTask.Result;
                return (streetTask.Result, postal.code, postal.city);
            }

            // Fallback når det ikke er en Vegadresse
            var fallbackStreet = await _matrikkelBruksenhetService.GetAddressForBruksenhet(id.value).ConfigureAwait(false);
            return (fallbackStreet, (string)null, (string)null);
        }

        private async Task<(string street, string postal, string city)?> FetchFromMatrikkelAddressAsync(AdresseServiceAdresseId addr)
        {
            var matrikkelAdress = await _matrikkelStoreClient.GetAdresse(addr.value).ConfigureAwait(false);

            if (matrikkelAdress is not Vegadresse roadAddress)
                return null;

            var streetTask = BuildVegadresseString(roadAddress);
            var postalTask = GetPostalInformation(matrikkelAdress.kretsIds);
            await Task.WhenAll(streetTask, postalTask).ConfigureAwait(false);

            var postal = postalTask.Result;
            return (streetTask.Result, postal.code, postal.city);
        }
    }
}
