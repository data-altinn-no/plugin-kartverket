using Altinn.Dan.Plugin.Kartverket.Clients;
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
        public Task<KartverketResponse> GetDDGrunnbok(string ssn);
    }

    public class DDWrapper : IDDWrapper
    {
        private readonly ILandbrukClient _landbrukClient;
        private readonly IKartverketClient _kartverketClient;
        private readonly IAddressLookupClient _geonorgeClient;
        private readonly ILogger _logger;
        private readonly ApplicationSettings _settings;

        public DDWrapper(LandbrukClient landbrukClient, KartverketClient kartverketClient, IAddressLookupClient geoNorgeClient, ILogger<DDWrapper> logger, IOptions<ApplicationSettings> settings)
        {
            _landbrukClient = landbrukClient;
            _logger = logger;
            _geonorgeClient = geoNorgeClient;
            _kartverketClient = kartverketClient;
            _settings = settings.Value;
        }

        public async Task<KartverketResponse> GetDDGrunnbok(string ssn)
        {
            var grunnbokResponse = new KartverketResponse
            {
                PropertyRights = new PropertyRights
                {
                    Properties = MapToInternal(await _kartverketClient.FindRegisterenhetsrettsandelerForPerson(ssn)),
                    PropertiesWithRights = MapToInternal(await _kartverketClient.FindRettigheterForPerson(ssn))
                }
            };

            return await _geonorgeClient.Get(await _landbrukClient.Get(grunnbokResponse));
        }

        private IEnumerable<Property> MapToInternal(RegisterenhetsrettsandelerResponse registerenhetsrettResponse)
        {
            return registerenhetsrettResponse
                .Registerenhetsrettsandeler
                .Select(x =>
                {
                    var property = MapToInternal(x.Registerenhetsrett);
                    property.FractionOwnership = x.Teller + "/" + x.Nevner;

                    return property;
                });
        }

        private IEnumerable<PropertyWithRights> MapToInternal(RettigheterResponse rettigheterResponse)
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
                        var propertyWithRights = ToPropertyWithRights(unitRights);
                        propertyWithRights.Rights.Add(rights);
                        properties.TryAdd(key, propertyWithRights);
                    }
                }
            }

            return properties.Values.ToList();
        }

        private Property MapToInternal(Registerenhetsrett registerenhetsRett)
        {
            bool isTestEnv = _settings.IsTestEnv;
            var unit = registerenhetsRett?.Registerenhet;
            var property = new Property
            {
                Type = registerenhetsRett?.Registerenhetsrettstype?.Navn,
                Address = isTestEnv ? "Testveien 8" : null,
                City = isTestEnv ? "Testeby" : null,
                PostalCode = isTestEnv ? "0256" : null,
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

        private PropertyWithRights ToPropertyWithRights(Registerenhetsrett registerenhetsRett)
        {
            var property = MapToInternal(registerenhetsRett);

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
