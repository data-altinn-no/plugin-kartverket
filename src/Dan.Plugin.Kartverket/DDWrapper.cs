using Altinn.Dan.Plugin.Kartverket.Clients;
using Dan.Common.Models;
using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket
{
    public interface IDDWrapper
    {
        public Task<KartverketResponse> GetDDGrunnbok(string ssn, bool addressLookup = true, bool singleAddress = false);
        public Task<KartverketResponse> GetDDAdresser(int gnr, int bnr, int festenr, int seksjonsnr, string kommunenummer, bool single=false);
    }

    public class DDWrapper : IDDWrapper
    {
        private readonly ILandbrukClient _landbrukClient;
        private readonly IKartverketClient _kartverketClient;
        private readonly IAddressLookupClient _geonorgeClient;
        private readonly ILogger _logger;
        private readonly ApplicationSettings _settings;
        private readonly IKartverketGrunnbokMatrikkelService _kartverketGrunnbokMatrikkelService;

        public DDWrapper(LandbrukClient landbrukClient, KartverketClient kartverketClient, IAddressLookupClient geoNorgeClient, ILogger<DDWrapper> logger, IOptions<ApplicationSettings> settings, IKartverketGrunnbokMatrikkelService kartverketGrunnbokMatrikkelService)
        {
            _landbrukClient = landbrukClient;
            _logger = logger;
            _geonorgeClient = geoNorgeClient;
            _kartverketClient = kartverketClient;
            _settings = settings.Value;
            _kartverketGrunnbokMatrikkelService = kartverketGrunnbokMatrikkelService;
        }

        public async Task<KartverketResponse> GetDDGrunnbok(string ssn, bool addressLookup = true, bool singleAddress = false)
        {

            var props = await _kartverketClient.FindRegisterenhetsrettsandelerForPerson(ssn);
            var propsWithRights = await _kartverketClient.FindRettigheterForPerson(ssn);

            var grunnbokResponse = new KartverketResponse
            {
                PropertyRights = new PropertyRights
                {
                    Properties = await MapToInternal(props),
                    PropertiesWithRights = await MapToInternal(propsWithRights)
                }
            };

            if (addressLookup)
                return await _kartverketGrunnbokMatrikkelService.GetAddresses(await _landbrukClient.Get(grunnbokResponse), singleAddress);
            else
                return await _landbrukClient.Get(grunnbokResponse);
        }

        public async Task<KartverketResponse> GetDDAdresser(int gnr, int bnr, int festenr, int seksjonsnr, string kommunenummer, bool single=false)
        {
            var kartverketInput = new KartverketResponse
            {
                PropertyRights = new PropertyRights
                {
                    Properties = new List<Property>
                    {
                        new Property
                        {
                            HoldingNumber = gnr.ToString(),
                            SubholdingNumber = bnr.ToString(),
                            LeaseNumber = festenr == 0 ? null : festenr.ToString(),
                            SectionNumber = seksjonsnr == 0 ? null : seksjonsnr.ToString(),
                            MunicipalityNumber = kommunenummer
                        }
                    },
                    PropertiesWithRights = new List<PropertyWithRights>()
                }                
            };
            return await _kartverketGrunnbokMatrikkelService.GetAddresses(kartverketInput, single);
        }
     

        private async Task<IEnumerable<Property>> MapToInternal(RegisterenhetsrettsandelerResponse registerenhetsrettResponse)
        {

            string a = "";
            var res = registerenhetsrettResponse
                .Registerenhetsrettsandeler
                .Select(async x =>                
                {
                    var property = await MapToInternal(x.Registerenhetsrett);
                    property.FractionOwnership = x.Teller + "/" + x.Nevner;

                    return property;
                });

            return await Task.WhenAll(res);            
        }

        private async Task<IEnumerable<PropertyWithRights>> MapToInternal(RettigheterResponse rettigheterResponse)
        {
            var properties = new Dictionary<string, PropertyWithRights>();
            foreach (var right in rettigheterResponse.Rettigheter)
            {
                var rights = ToRightModel(right);
                foreach (var unitRights in right.InvolvererRegisterenhetsretter)
                {
                    var unit = unitRights.Registerenhet as Matrikkelenhet;
                    if (unit == null)
                    {
                        continue;
                    }

                    var key = $"{unit.Kommune?.Navn}{unit.Gaardsnummer}{unit.Bruksnummer}{unit.Festenummer}{unit.Seksjonsnummer}";

                    var containsKey = properties.TryGetValue(key, out var property);
                    if (containsKey)
                    {
                        property.Rights.Add(rights);
                    }
                    else
                    {
                        var propertyWithRights = await ToPropertyWithRights(unitRights);
                        propertyWithRights.Rights.Add(rights);
                        properties.TryAdd(key, propertyWithRights);
                    }
                }
            }

            return properties.Values.ToList();
        }

        private async Task<Property> MapToInternal(Registerenhetsrett registerenhetsRett)
        {
            bool isTestEnv = _settings.IsTestEnv;
            var unit = registerenhetsRett?.Registerenhet;
            var property = new Property
            {
                Type = registerenhetsRett?.Registerenhetsrettstype?.Navn,
                Address =  null,
                City = null,
                PostalCode = null,
                AddressList = new List<string>()
            };
            if (unit is Matrikkelenhet cadastreUnit)
            {
                property.HoldingNumber = cadastreUnit?.Gaardsnummer?.ToString();
                property.SubholdingNumber = cadastreUnit?.Bruksnummer?.ToString();
                property.LeaseNumber = cadastreUnit?.Festenummer?.ToString();
                property.SectionNumber = cadastreUnit?.Seksjonsnummer?.ToString();
                property.MunicipalityNumber = cadastreUnit?.Kommune.Kommunenummer;
                property.Municipality = cadastreUnit?.Kommune.Navn;
            }
            else if (unit is Borettslagsandel hoaUnit)
            {
                var borettslag = await _kartverketClient.FindAdresseForBorettslagsandel(hoaUnit.Borettslag?.Organisasjonsnummer, hoaUnit.Andelsnummer);

                var address = borettslag?.Adresse.Vegadresse.Adressenavn + " " + borettslag?.Adresse.Vegadresse.Husnummer + borettslag?.Adresse?.Vegadresse?.Bokstav;

                property.Address = borettslag?.Adresse.Vegadresse.Adressenavn + " " + borettslag?.Adresse.Vegadresse.Husnummer + borettslag?.Adresse?.Vegadresse?.Bokstav;
                property.AddressList.Add(property.Address);
                property.MunicipalityNumber = borettslag?.Adresse.Vegadresse.Kommune.Kommunenummer;
                property.Municipality = borettslag?.Adresse.Vegadresse.Kommune.Navn;

                var postalcodes = await _geonorgeClient.Search(address, borettslag?.Adresse.Vegadresse.Kommune.Kommunenummer, borettslag?.Adresse.Vegadresse.Bolignummer);
                if (postalcodes?.Adresser?.Count > 1)
                    _logger.LogWarning($"Geonorge returned multiple addresses for {address}/{borettslag?.Adresse.Vegadresse.Kommune.Kommunenummer}/{borettslag?.Adresse.Vegadresse.Bolignummer}");

                property.PostalCode = postalcodes?.Adresser?.FirstOrDefault()?.Postnummer;
                property.City = postalcodes?.Adresser?.FirstOrDefault()?.Poststed;
            }

            return property;
        }

        private static Right ToRightModel(RettighetOgInvolverte rettighet)
            => new Right
            {
                DocumentNumber = rettighet.Rettighet.Dokumentnummer?.ToString(),
                DocumentYear = rettighet.Rettighet.Dokumentaar?.ToString(),
                JudgementNumber = rettighet.Rettighet.Rettsstiftelsesnummer?.ToString(),
                JudgmentType = rettighet.Rettighet.Rettsstiftelsetype?.ToString(),
                OfficeNumber = rettighet.Rettighet.Embetenummer,
            };

        private async Task<PropertyWithRights> ToPropertyWithRights(Registerenhetsrett registerenhetsRett)
        {
            var property = await MapToInternal(registerenhetsRett);

            return new PropertyWithRights
            {
                City = property.City,
                Type = property.Type,
                PostalCode = property.PostalCode,
                SubholdingNumber = property.SubholdingNumber,
                SectionNumber = property.SectionNumber,
                FractionOwnership = property.FractionOwnership,
                HoldingNumber = property.HoldingNumber,
                Municipality = property.Municipality,
                MunicipalityNumber = property.MunicipalityNumber,
                LeaseNumber = property.LeaseNumber,
                Address = property.Address,
                Rights = new List<Right>()
            };
        }

    }
}
