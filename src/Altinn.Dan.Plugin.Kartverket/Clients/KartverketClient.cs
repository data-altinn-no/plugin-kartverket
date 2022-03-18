using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Kartverket.Config;
using Altinn.Dan.Plugin.Kartverket.Models;
using Microsoft.Extensions.Options;
using Nadobe.Common.Exceptions;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Kartverket.Clients
{
    public class KartverketClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationSettings _settings;
        private readonly ILandbrukClient _landbrukClient;
        private readonly IAddressLookupClient _addressLookupClient;

        public KartverketClient(
            IHttpClientFactory httpClientFactory,
            IOptions<ApplicationSettings> settings,
            ILandbrukClient landbrukClient,
            IAddressLookupClient addressLookupClient)
        {
            _httpClient = httpClientFactory.CreateClient("KartverketClient");
            _settings = settings.Value;
            _landbrukClient = landbrukClient;
            _addressLookupClient = addressLookupClient;
        }

        public async Task<KartverketResponse> Get(string ssn)
        {
            if (string.IsNullOrEmpty(ssn.Trim()))
            {
                throw new EvidenceSourcePermanentClientException(Metadata.ERROR_CCR_UPSTREAM_ERROR, $"Bad request (ssn cannot be empty)");
            }

            var grunnbokResponse = new KartverketResponse
            {
                PropertyRights = new PropertyRights
                {
                    Properties = MapToInternal(await FindRegisterenhetsrettsandelerForPerson(ssn)),
                    PropertiesWithRights = MapToInternal(await FindRettigheterForPerson(ssn))
                }
            };

            return await _addressLookupClient.Get(await _landbrukClient.Get(grunnbokResponse));
        }

        private async Task<RegisterenhetsrettsandelerResponse> FindRegisterenhetsrettsandelerForPerson(string ssn)
        {
            return await MakeRequest<RegisterenhetsrettsandelerResponse>(_settings.KartverketRegisterenhetsrettsandelerForPersonUrl, ssn);
        }

        private async Task<RettigheterResponse> FindRettigheterForPerson(string ssn)
        {
            return await MakeRequest<RettigheterResponse>(_settings.KartverketRettigheterForPersonUrl, ssn);
        }

        private async Task<T> MakeRequest<T>(string url, string ssn) where T : new()
        {
            HttpResponseMessage response = null;
            try
            {
                var completeUrl = url.Replace("{identifikasjonsnummer}", Uri.EscapeDataString(ssn));
                response = await _httpClient.GetAsync(completeUrl);
                var responseData = await response.Content.ReadAsStringAsync();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    {
                        return JsonConvert.DeserializeObject<T>(responseData);
                    }
                    default:
                    {
                        throw new EvidenceSourcePermanentClientException(Metadata.ERROR_CCR_UPSTREAM_ERROR,
                            $"External API call to Kartverket failed ({(int)response.StatusCode} - {response.StatusCode})" + (string.IsNullOrEmpty(responseData) ? string.Empty : $", details: {responseData}"));
                    }
                }
            }
            catch (HttpRequestException e)
            {
                throw new EvidenceSourcePermanentServerException(Metadata.ERROR_CCR_UPSTREAM_ERROR, null, e);
            }
            finally
            {
                response?.Dispose();
            }
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
