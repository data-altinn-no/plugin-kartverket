using Dan.Common.Exceptions;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces;
using Dan.Plugin.Kartverket.Clients.Matrikkel;
using Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces;
using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using Kartverket.Matrikkel.StoreService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

                // Bulk-hent alle bruksenheter, adresser, veger og kretser i ett StoreService-kall per type
                var allResults = await FetchAddressTuples(bruksenhetListTask.Result, matrikkelAddrListTask.Result).ConfigureAwait(false);

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

        private async Task<(string code, string city)> GetPostalInformation(KretsId[] kretsList)
        {
            if (kretsList == null || kretsList.Length == 0)
                return ("", "");

            // One (cached) bulk lookup for the whole krets list instead of one call per id
            var kretser = (await _matrikkelStoreClient.GetKretser(kretsList.Select(k => k.value)))
                .ToDictionary(k => k.id.value);

            return GetPostalInformation(kretsList, kretser);
        }

        private static (string code, string city) GetPostalInformation(KretsId[] kretsList, IReadOnlyDictionary<long, Krets> kretser)
        {
            foreach (var kretsid in kretsList ?? Array.Empty<KretsId>())
            {
                if (kretser.TryGetValue(kretsid.value, out var krets) && krets is Postnummeromrade postnummeromrade)
                {
                    return (postnummeromrade.kretsnummer.ToString(), postnummeromrade.kretsnavn);
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
            var ident = await _identServiceClient.GetPersonIdentity(identifier);
            var registerRettsAndelList = await _regRettsandelsClientService.GetAndelerForRettighetshaver(ident);

            var propertyTasks = registerRettsAndelList.Select(GetPropertiesWithOwners);
            var properties = await Task.WhenAll(propertyTasks);
            return properties.Where(propertyWithOwner => propertyWithOwner != null).ToList();
        }

        private async Task<PropertyWithOwners> GetPropertiesWithOwners(string registerenhetsrettsandelid)
        {
            try
            {
                var regenhetsandelfromstore = await _storeServiceClient.GetRettighetsandeler(registerenhetsrettsandelid);

                //Skip if null or if property is historical, as we only want currently owned properties.
                if (regenhetsandelfromstore == null || regenhetsandelfromstore.historisk)
                    return null;

                var matrikkelenhetgrunnbok = await _storeServiceClient.GetMatrikkelEnhetFromRegisterRettighetsandel(regenhetsandelfromstore.registerenhetsrettId.value);

                if (matrikkelenhetgrunnbok == null)
                    return null;

                // Start GetKommune early — runs concurrently with co-owner logic below
                var kommuneTask = _storeServiceClient.GetKommune(matrikkelenhetgrunnbok.kommuneId.value);

                // Find co-owners
                var listOfCoOwners = new List<CoOwner>();
                var share = $"{regenhetsandelfromstore.teller}/{regenhetsandelfromstore.nevner}";
                if (regenhetsandelfromstore.teller != regenhetsandelfromstore.nevner)
                {
                    var registerenhetId = matrikkelenhetgrunnbok.id.value;

                    var registerEnhetTilRegisterenhetsrettId = await _registerenhetsrettClientService.GetRetterForEnheter(registerenhetId);

                    var registerEnhetIdTilRegisterenhetsrettIds = registerEnhetTilRegisterenhetsrettId?.Values?
                        .SelectMany(rettid => rettid.Select(ids => ids.value))
                        .ToList() ?? new List<string>();

                    foreach (var registerEnhetId in registerEnhetIdTilRegisterenhetsrettIds)
                    {
                        var andelerIRetter = await _regRettsandelsClientService.GetAndelerIRetter(registerEnhetId);

                        var andelerIRetterValues = andelerIRetter?.Body?.@return?.Values;

                        var firstAndel = andelerIRetterValues?.FirstOrDefault();
                        if (firstAndel == null)
                            continue;

                        var andelTasks = firstAndel.Select(async andel =>
                        {
                            var andeler = await _storeServiceClient.GetRettighetsandeler(andel.value.ToString());

                            if (andeler == null || andeler.historisk)
                                return null;

                            var coOwner = await _storeServiceClient.GetPerson(andeler.rettighetshaverId.value);
                            if (coOwner == null)
                                return null;

                            return new CoOwner()
                            {
                                Identifier = coOwner.identifikasjonsnummer ?? null,
                                Name = coOwner.navn ?? null,
                                OwnerShare = $"{andeler.teller}/{andeler.nevner}" ?? null
                            };
                        });

                        var coOwners = await Task.WhenAll(andelTasks);
                        listOfCoOwners.AddRange(coOwners.Where(c => c != null));
                    }
                }
                else
                {
                    //for single owners
                    var owner = await _storeServiceClient.GetPerson(regenhetsandelfromstore.rettighetshaverId.value);

                    if (owner != null)
                    {
                        listOfCoOwners.Add(new CoOwner
                        {
                            Identifier = owner.identifikasjonsnummer ?? null,
                            Name = owner.navn ?? null,
                            OwnerShare = $"{regenhetsandelfromstore.teller}/{regenhetsandelfromstore.nevner}" ?? null
                        });
                    }
                }                

                // GetKommune has been running concurrently — collect result now
                var kommune = await kommuneTask ?? new Models.Kommune();

                // Find Address and if the property is a fritidsbolig
                var matrikkelenhetid = await _matrikkelenhetServiceClient.GetMatrikkelenhet(matrikkelenhetgrunnbok.gaardsnummer, matrikkelenhetgrunnbok.bruksnummer, matrikkelenhetgrunnbok.festenummer, matrikkelenhetgrunnbok.seksjonsnummer, kommune.Number);

                if (matrikkelenhetid == null)
                    return null;

                var bruksenhetIds = await _matrikkelBruksenhetService.GetBruksenheter(matrikkelenhetid.value);

                var addresseList = new List<Address>();
                var boligType = new List<string>();

                var matrikkelAdresseTask = GetAddressByMatrikkelenhetId(matrikkelenhetid.value);

                if (bruksenhetIds?.Any() == true)
                {
                    // Bulk-fetch all bruksenheter in one call, then resolve address/type per bruksenhet
                    var bruksenheter = await _matrikkelStoreClient.GetBruksenheter(bruksenhetIds.Select(id => id.value));
                    var bruksenhetResultsTask = Task.WhenAll(bruksenheter.Select(b => GetAddressAndBoligTypeByBruksenhet(b, matrikkelenhetid.value)));

                    await Task.WhenAll(bruksenhetResultsTask, matrikkelAdresseTask);

                    foreach (var (address, type) in bruksenhetResultsTask.Result)
                    {
                        if (address != null) addresseList.Add(address);
                        if (type != null) boligType.Add(type);
                    }
                }
                else
                {
                    await matrikkelAdresseTask;
                }

                //we need to get the address one more time in case the bruksenhet didn't have a addresseId
                if (matrikkelAdresseTask.Result != null)
                    addresseList.Add(matrikkelAdresseTask.Result);

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

                return new PropertyWithOwners()
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
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when calling FindOwnedProperties");
                return null;
            }
        }

        private async Task<(Address address, string boligType)> GetAddressAndBoligTypeByBruksenhet(Bruksenhet bruksenhet, long matrikkelenhetId)
        {
            if (bruksenhet == null)
                return (null, null);

            var bruksenhetsTypeTask = bruksenhet.bruksenhetstypeKodeId != null
                ? _matrikkelStoreClient.GetBruksenhetstype(bruksenhet.bruksenhetstypeKodeId.value)
                : Task.FromResult<BruksenhetstypeKode>(null);

            var adresseTask = bruksenhet.adresseId != null
                ? GetAddresseByBruksenhet(bruksenhet, matrikkelenhetId)
                : Task.FromResult<Address>(null);

            await Task.WhenAll(bruksenhetsTypeTask, adresseTask);

            return (adresseTask.Result, bruksenhetsTypeTask.Result?.kodeverdi);
        }

        public async Task<List<PropertyModel>> FindProperties(string identifier)
        {
            //Get grunnbok identifier for
            var ident = await _identServiceClient.GetPersonIdentity(identifier);

            //Get all properties owned by identifier
            var regrettsandelListe = await _regRettsandelsClientService.GetAndelerForRettighetshaver(ident);

            var numbersToRemove = Math.Min(10, regrettsandelListe.Count);
            regrettsandelListe.RemoveRange(numbersToRemove, regrettsandelListe.Count - numbersToRemove);

            // The properties are independent of each other — resolve them concurrently.
            // Task.WhenAll preserves the input order in the result array.
            var propertyModels = await Task.WhenAll(regrettsandelListe.Select(GetPropertyModel));
            return propertyModels.ToList();
        }

        private async Task<PropertyModel> GetPropertyModel(string registerenhetsrettsandelid)
        {
            var regenhetsandelfromstore = await _storeServiceClient.GetRettighetsandeler(registerenhetsrettsandelid);

            var matrikkelenhetgrunnbok = await _storeServiceClient.GetMatrikkelEnhetFromRegisterRettighetsandel(regenhetsandelfromstore.registerenhetsrettId.value);

            // Heftelser, ownership info and kommune only depend on matrikkelenhetgrunnbok — run them concurrently
            var heftelserTask = GetHeftelserWithPawnOwnerNames(matrikkelenhetgrunnbok.id.value);
            var ownershipTransferTask = _informasjonsServiceClientService.GetOwnershipInfo(matrikkelenhetgrunnbok.id.value);
            var kommuneTask = _storeServiceClient.GetKommune(matrikkelenhetgrunnbok.kommuneId.value);

            var kommune = await kommuneTask;

            var matrikkelenhetid =
                await _matrikkelenhetServiceClient.GetMatrikkelenhet(matrikkelenhetgrunnbok.gaardsnummer, matrikkelenhetgrunnbok.bruksnummer, matrikkelenhetgrunnbok.festenummer, matrikkelenhetgrunnbok.seksjonsnummer, kommune.Number);

            // The teig lookup is independent of the building lookups — run it alongside them
            var matrikkelEnhetTask = _matrikkelenhetServiceClient.GetMatrikkelEnhetTeig(matrikkelenhetgrunnbok.gaardsnummer, matrikkelenhetgrunnbok.bruksnummer,
                matrikkelenhetgrunnbok.festenummer, matrikkelenhetgrunnbok.seksjonsnummer, kommune.Number);

            var bygningsider = await _matrikkelbygningClientService.GetBygningerForMatrikkelenhet(matrikkelenhetid.value);

            // One bulk call for all buildings instead of one call per id; missing buildings
            // are omitted, matching the previous behaviour of contributing 0 to the sum
            var bygninger = await _matrikkelStoreClient.GetBygninger(bygningsider);
            var buildings = bygninger.Select(b => b.bebygdAreal).ToList();

            var matrikkelEnhet = await matrikkelEnhetTask;
            var heftelserFromRettsstiftelse = await heftelserTask;
            var ownershipTransfer = await ownershipTransferTask;

            return new PropertyModel()
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
            };
        }

        private async Task<List<PawnDocument>> GetHeftelserWithPawnOwnerNames(string registerenhetid)
        {
            var heftelser = await _rettsstiftelseClientService.GetHeftelser(registerenhetid);
            return await _storeServiceClient.GetPawnOwnerNames(heftelser);
        }

        public async Task<bool> PropertyHasFritidsbolig(string matrikkelNumber)
        {
            var matrikkelnummerSplit = matrikkelNumber.Split('-', '/');
            var kommunenr = matrikkelnummerSplit[0];
            var gnr = matrikkelnummerSplit.Length > 1 ? Convert.ToInt32(matrikkelnummerSplit[1]) : 0;
            var bnr = matrikkelnummerSplit.Length > 2 ? Convert.ToInt32(matrikkelnummerSplit[2]) : 0;
            var fnr = matrikkelnummerSplit.Length > 3 ? Convert.ToInt32(matrikkelnummerSplit[3]) : 0;
            var snr = matrikkelnummerSplit.Length > 4 ? Convert.ToInt32(matrikkelnummerSplit[4]) : 0;

            var matrikkelenhetid = await _matrikkelenhetServiceClient.GetMatrikkelenhet(gnr, bnr, fnr, snr, kommunenr);
            var bruksenhetIder = await _matrikkelBruksenhetService.GetBruksenheter(matrikkelenhetid.value);

            // Bulk-fetch all bruksenheter, then their distinct (cached) type codes
            var bruksenheter = await _matrikkelStoreClient.GetBruksenheter(bruksenhetIder.Select(id => id.value));

            var bruksenhetstypeIds = bruksenheter
                .Where(b => b.bruksenhetstypeKodeId != null)
                .Select(b => b.bruksenhetstypeKodeId.value)
                .Distinct();

            var bruksenhetstyper = await _matrikkelStoreClient.GetBruksenhetstyper(bruksenhetstypeIds);

            return bruksenhetstyper.Any(t => t.kodeverdi.Equals("F"));
        }

        public async Task<List<Address>> GetAdresseByMatrikkelNumber(string matrikkelNumber)
        {
            var matrikkelnummerSplit = matrikkelNumber.Split('-', '/');
            var kommunenr = matrikkelnummerSplit[0];
            var gnr = matrikkelnummerSplit.Length > 1 ? Convert.ToInt32(matrikkelnummerSplit[1]) : 0;
            var bnr = matrikkelnummerSplit.Length > 2 ? Convert.ToInt32(matrikkelnummerSplit[2]) : 0;
            var fnr = matrikkelnummerSplit.Length > 3 ? Convert.ToInt32(matrikkelnummerSplit[3]) : 0;
            var snr = matrikkelnummerSplit.Length > 4 ? Convert.ToInt32(matrikkelnummerSplit[4]) : 0;

            var matrikkelenhetid = await _matrikkelenhetServiceClient.GetMatrikkelenhet(gnr, bnr, fnr, snr, kommunenr);

            var adresseList = new List<Address>();

            var adresse = await GetAddressByMatrikkelenhetId(matrikkelenhetid.value);
            if(adresse != null)
            {
                adresseList.Add(adresse);
            }

            var bruksenhetIder = await _matrikkelBruksenhetService.GetBruksenheter(matrikkelenhetid.value);
            var bruksenheter = (await _matrikkelStoreClient.GetBruksenheter(bruksenhetIder.Select(id => id.value)))
                .ToDictionary(b => b.id.value);

            foreach (var bruksenhetId in bruksenhetIder)
            {
                if (bruksenheter.TryGetValue(bruksenhetId.value, out var bruksenhet) && bruksenhet.adresseId != null)
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
            var adresser = (await _matrikkelStoreClient.GetAdresser(matrikkelEnhetAddresseListe.Select(a => a.value)))
                .ToDictionary(a => a.id.value);

            foreach(var adresse in matrikkelEnhetAddresseListe)
            {
                if (!adresser.TryGetValue(adresse.value, out var matrikkelAdress))
                    continue;

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
                var matrikkelAdresser = (await _matrikkelStoreClient.GetAdresser(matrikkelEnhetAddresseListe.Select(a => a.value)))
                    .ToDictionary(a => a.id.value);
                foreach (var add in matrikkelEnhetAddresseListe)
                {
                    if (!matrikkelAdresser.TryGetValue(add.value, out var matrikkelAdress))
                        continue;

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

        /// <summary>
        /// Henter alle bruksenheter, adresser, veger og kretser for en matrikkelenhet med ett
        /// StoreService-kall per type i stedet for ett kall per id, og bygger adressetupler i samme
        /// rekkefølge som før: alle bruksenheter, deretter matrikkelenhet-adressene i omvendt rekkefølge.
        /// </summary>
        private async Task<List<(string street, string postal, string city)?>> FetchAddressTuples(
            BruksenhetServiceBruksenhetId[] bruksenhetIds, AdresseServiceAdresseId[] matrikkelAddrIds)
        {
            var bruksenheter = (await _matrikkelStoreClient.GetBruksenheter(bruksenhetIds.Select(id => id.value)).ConfigureAwait(false))
                .ToDictionary(b => b.id.value);

            var adresseIds = bruksenheter.Values
                .Where(b => b.adresseId != null)
                .Select(b => b.adresseId.value)
                .Concat(matrikkelAddrIds.Select(a => a.value))
                .Distinct();

            var adresser = (await _matrikkelStoreClient.GetAdresser(adresseIds).ConfigureAwait(false))
                .ToDictionary(a => a.id.value);

            // Veger og kretser er cachede referansedata — hent alle i ett kall per type
            var vegadresser = adresser.Values.OfType<Vegadresse>().ToList();
            var vegerTask = _matrikkelStoreClient.GetVeger(vegadresser.Where(v => v.vegId != null).Select(v => v.vegId.value));
            var kretserTask = _matrikkelStoreClient.GetKretser(vegadresser.SelectMany(v => v.kretsIds ?? Array.Empty<KretsId>()).Select(k => k.value));
            await Task.WhenAll(vegerTask, kretserTask).ConfigureAwait(false);
            var veger = vegerTask.Result.ToDictionary(v => v.id.value);
            var kretser = kretserTask.Result.ToDictionary(k => k.id.value);

            var bruksenhetTasks = bruksenhetIds.Select(async id =>
            {
                bruksenheter.TryGetValue(id.value, out var bruksenhet);
                if (bruksenhet?.adresseId == null)
                    return ((string street, string postal, string city)?)null;

                adresser.TryGetValue(bruksenhet.adresseId.value, out var adresse);
                if (adresse is Vegadresse roadAddress)
                    return BuildAddressTuple(roadAddress, veger, kretser);

                // Fallback når det ikke er en Vegadresse
                var fallbackStreet = await _matrikkelBruksenhetService.GetAddressForBruksenhet(id.value).ConfigureAwait(false);
                return (fallbackStreet, (string)null, (string)null);
            });

            var bruksenhetResults = await Task.WhenAll(bruksenhetTasks).ConfigureAwait(false);

            var matrikkelResults = matrikkelAddrIds.Reverse().Select(addr =>
            {
                adresser.TryGetValue(addr.value, out var adresse);
                return adresse is Vegadresse roadAddress ? BuildAddressTuple(roadAddress, veger, kretser) : null;
            });

            return bruksenhetResults.Concat(matrikkelResults).ToList();
        }

        private static (string street, string postal, string city)? BuildAddressTuple(
            Vegadresse roadAddress, IReadOnlyDictionary<long, Veg> veger, IReadOnlyDictionary<long, Krets> kretser)
        {
            Veg veg = null;
            if (roadAddress.vegId != null)
                veger.TryGetValue(roadAddress.vegId.value, out veg);

            var street = veg == null ? string.Empty : veg.adressenavn + " " + roadAddress.nummer + roadAddress.bokstav;
            var (code, city) = GetPostalInformation(roadAddress.kretsIds, kretser);
            return (street, code, city);
        }
    }
}
