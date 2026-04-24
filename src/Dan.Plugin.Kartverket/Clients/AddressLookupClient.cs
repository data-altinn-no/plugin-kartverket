using Altinn.App.ExternalApi.AddressLookup;
using Dan.Common.Exceptions;
using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients
{
    public interface IAddressLookupClient
    {
        public Task<KartverketResponse> Get(KartverketResponse kartverket);
        public Task<OutputAdresseList> Search(string address, string municipalityNo, string flatNo, string city);
        public Task<List<List<List<double>>>> GetCoordinatesForProperty(
            string? matrikkelNumber = null,
            string? gnr = null,
            string? bnr = null,
            string? snr = null,
            string? fnr = null,
            string? kommunenr = null,
            bool includeWholeOmrade = false);

        public Task<OutputAdresseList> SearchByMatrikkelNumber(
            string municipalityNumber,
            string holdingNumber,
            string subholdingNumber,
            string leaseNumber,
            string streetName,
            string postalCode,
            string postalCity //poststed, not kommunenavn
        );



        public class AddressLookupClient : IAddressLookupClient
        {
            private readonly HttpClient _httpClient;
            private readonly ApplicationSettings _settings;
            private IKartverketGrunnbokMatrikkelService _kartverketGrunnbokMatrikkelService;

            public AddressLookupClient(IHttpClientFactory httpClientFactory, IOptions<ApplicationSettings> settings, IKartverketGrunnbokMatrikkelService matrikkelService)
            {
                _httpClient = httpClientFactory.CreateClient("SafeHttpClient");
                _settings = settings.Value;
                _kartverketGrunnbokMatrikkelService = matrikkelService;
            }

            public async Task<KartverketResponse> Get(KartverketResponse kartverket)
            {
                List<Property> properties = new List<Property>();
                foreach (Property property in kartverket.PropertyRights.Properties)
                {
                    properties.Add(property);
                    await UpdateProperty(property, await SokAdresse(property));
                }

                kartverket.PropertyRights.Properties = properties;

                List<PropertyWithRights> propertyWithRights = new List<PropertyWithRights>();
                foreach (PropertyWithRights property in kartverket.PropertyRights.PropertiesWithRights)
                {
                    propertyWithRights.Add(property);
                    await UpdateProperty(property, await SokAdresse(property));
                }

                kartverket.PropertyRights.PropertiesWithRights = propertyWithRights;

                return kartverket;
            }

            public async Task<List<List<List<double>>>> GetCoordinatesForProperty(
    string matrikkelNumber = null,
    string gnr = null,
    string bnr = null,
    string snr = null,
    string fnr = null,
    string kommunenr = null,
    bool includeWholeOmrade = false)
            {
                var urlBuilder = new StringBuilder();
                urlBuilder.Append(_settings.CoordinatesLookupUrl).Append("/geokoding");

                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(matrikkelNumber))
                    queryParams.Add("matrikkelnummer=" + Uri.EscapeDataString(matrikkelNumber));
                if (!string.IsNullOrEmpty(gnr))
                    queryParams.Add("gardsnummer=" + Uri.EscapeDataString(gnr));
                if (!string.IsNullOrEmpty(bnr))
                    queryParams.Add("bruksnummer=" + Uri.EscapeDataString(bnr));
                if (!string.IsNullOrEmpty(snr))
                    queryParams.Add("seksjonsnummer=" + Uri.EscapeDataString(snr));
                if (!string.IsNullOrEmpty(fnr))
                    queryParams.Add("festenummer=" + Uri.EscapeDataString(fnr));
                if (!string.IsNullOrEmpty(kommunenr))
                    queryParams.Add("kommunenummer=" + Uri.EscapeDataString(kommunenr));

                queryParams.Add($"omrade={(includeWholeOmrade ? "true" : "false")}");
                queryParams.Add("utkoordsys=4258");

                urlBuilder.Append("?").Append(string.Join("&", queryParams));

                try
                {
                    var response = await _httpClient.GetAsync(urlBuilder.ToString());
                    response.EnsureSuccessStatusCode();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var geocodingResponse =
                        await response.Content.ReadFromJsonAsync<GeocodingResponse>(options);

                    if (geocodingResponse?.Features == null)
                        return new List<List<List<double>>>();

                    var coordinates = geocodingResponse.Features
                        .Where(f => f?.Geometry?.Coordinates.ValueKind == JsonValueKind.Array)
                        .SelectMany(f => NormalizeCoordinates(f.Geometry.Coordinates))
                        .ToList();

                    return coordinates;
                }
                catch (Exception e)
                {
                    throw new EvidenceSourcePermanentServerException(
                        Metadata.ERROR_CCR_UPSTREAM_ERROR,
                        null,
                        e);
                }
            }


            public async Task<OutputAdresseList> Search(string address, string municipalityNo, string flatNo, string city)
            {
                HttpResponseMessage response = null;

                var urlBuilder = new StringBuilder();
                urlBuilder.Append(_settings.AddressLookupUrl).Append("/sok?");
                urlBuilder.Append("fuzzy=false").Append("&");


                if (!string.IsNullOrEmpty(municipalityNo))
                {
                    urlBuilder.Append("kommunenummer=").Append(int.Parse(municipalityNo).ToString("d4")).Append('&');
                }
                if (!string.IsNullOrEmpty(address))
                {
                    urlBuilder.Append("adressetekst=").Append(address).Append('&');
                }
                if (!string.IsNullOrEmpty(flatNo))
                {
                    urlBuilder.Append("bruksenhetsnummer=").Append(flatNo).Append('&');
                }
                if(!string.IsNullOrEmpty(city))
                {
                    urlBuilder.Append("kommunenavn=").Append(city).Append('&');
                }

                try
                {
                    urlBuilder.Append("treffPerSide=100").Append('&');
                    urlBuilder.Length--;

                    response = await _httpClient.GetAsync(urlBuilder.ToString());
                    var responseData = await response.Content.ReadAsStringAsync();
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            {
                                return JsonConvert.DeserializeObject<OutputAdresseList>(responseData);
                            }
                        default:
                            {
                                throw new EvidenceSourcePermanentClientException(Metadata.ERROR_CCR_UPSTREAM_ERROR,
                                    $"External API call to Geonorge failed ({(int)response.StatusCode} - {response.StatusCode})" + (string.IsNullOrEmpty(responseData) ? string.Empty : $", details: {responseData}"));
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

            public async Task<OutputAdresseList> SearchByMatrikkelNumber(
                string municipalityNumber,
                string holdingNumber,
                string subholdingNumber,
                string leaseNumber,
                string addressText,
                string postalCode,
                string postalCity
            )
            {
                HttpResponseMessage response = null;

                var urlBuilder = new StringBuilder();
                urlBuilder.Append(_settings.AddressLookupUrl).Append("/sok?");
                urlBuilder.Append("fuzzy=false").Append("&");

                if (!string.IsNullOrEmpty(municipalityNumber))
                {
                    urlBuilder
                        .Append("kommunenummer=")
                        .Append(int.Parse(municipalityNumber).ToString("d4"))
                        .Append('&');
                }
                if (!string.IsNullOrEmpty(holdingNumber))
                {
                    urlBuilder.Append("gardsnummer=").Append(holdingNumber).Append('&');
                }
                if (!string.IsNullOrEmpty(subholdingNumber))
                {
                    urlBuilder.Append("bruksnummer=").Append(subholdingNumber).Append('&');
                }
                if (!string.IsNullOrEmpty(leaseNumber))
                {
                    urlBuilder.Append("festenummer=").Append(leaseNumber).Append('&');
                }
                if (!string.IsNullOrEmpty(addressText))
                {
                    urlBuilder.Append("adressetekst=").Append(Uri.EscapeDataString(addressText)).Append('&');
                }
                if (!string.IsNullOrEmpty(postalCode))
                {
                    urlBuilder.Append("postnummer=").Append(postalCode).Append('&');
                }
                if (!string.IsNullOrEmpty(postalCity))
                {
                    urlBuilder.Append("poststed=").Append(postalCity).Append('&');
                }

                try
                {
                    urlBuilder.Append("treffPerSide=100").Append('&');
                    urlBuilder.Length--;

                    response = await _httpClient.GetAsync(urlBuilder.ToString());
                    var responseData = await response.Content.ReadAsStringAsync();
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            {
                                return JsonConvert.DeserializeObject<OutputAdresseList>(responseData);
                            }
                        default:
                            {
                                throw new EvidenceSourcePermanentClientException(
                                    Metadata.ERROR_CCR_UPSTREAM_ERROR,
                                    $"External API call to Geonorge failed ({(int)response.StatusCode} - {response.StatusCode})"
                                        + (
                                            string.IsNullOrEmpty(responseData)
                                                ? string.Empty
                                                : $", details: {responseData}"
                                        )
                                );
                            }
                    }
                }
                catch (HttpRequestException e)
                {
                    throw new EvidenceSourcePermanentServerException(
                        Metadata.ERROR_CCR_UPSTREAM_ERROR,
                        null,
                        e
                    );
                }
                finally
                {
                    response?.Dispose();
                }
            }

            private async Task<OutputAdresseList> SokAdresse(Property property)
            {
                HttpResponseMessage response = null;

                var urlBuilder = new StringBuilder();
                urlBuilder.Append(_settings.AddressLookupUrl).Append("/sok?");

                if (!string.IsNullOrEmpty(property.MunicipalityNumber))
                {
                    urlBuilder.Append("kommunenummer=").Append(int.Parse(property.MunicipalityNumber).ToString("d4")).Append('&');
                }
                if (!string.IsNullOrEmpty(property.Address))
                {
                    urlBuilder.Append("adressetekst=").Append(property.Address).Append('&');
                }
                if (!string.IsNullOrEmpty(property.SubholdingNumber))
                {
                    urlBuilder.Append("bruksnummer=").Append(property.SubholdingNumber).Append('&');
                }
                if (!string.IsNullOrEmpty(property.HoldingNumber))
                {
                    urlBuilder.Append("gardsnummer=").Append(property.HoldingNumber).Append('&');
                }

                if (!string.IsNullOrEmpty(property.LeaseNumber))
                {
                    urlBuilder.Append("festenummer=").Append(Uri.EscapeDataString(property.LeaseNumber)).Append('&');
                }

                try
                {
                    urlBuilder.Append("treffPerSide=100").Append('&');
                    urlBuilder.Length--;

                    response = await _httpClient.GetAsync(urlBuilder.ToString());
                    var responseData = await response.Content.ReadAsStringAsync();
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            {
                                return JsonConvert.DeserializeObject<OutputAdresseList>(responseData);
                            }
                        default:
                            {
                                throw new EvidenceSourcePermanentClientException(Metadata.ERROR_CCR_UPSTREAM_ERROR,
                                    $"External API call to Geonorge failed ({(int)response.StatusCode} - {response.StatusCode})" + (string.IsNullOrEmpty(responseData) ? string.Empty : $", details: {responseData}"));
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

            private async Task UpdateProperty(Property property, OutputAdresseList addresses)
            {
                //TODO: Should this use 'FirstOrDefault'? (ex. Myrdalsvegen)

                if (addresses?.Adresser?.Count > 1)
                {
                    var matrikkelAddress = await _kartverketGrunnbokMatrikkelService.GetAddressForSection(int.Parse(property.HoldingNumber), int.Parse(property.SubholdingNumber), int.Parse(property.LeaseNumber), property.MunicipalityNumber, int.Parse(property.SectionNumber));
                    string tmp = string.Empty;

                    //Unit address contains H01234 as identifier
                    if (matrikkelAddress.Contains("-H"))
                    {
                        var addressSplit = matrikkelAddress.Split("-");
                        //Matrikkel returns address text with flat number as "H01234" - if this is the case we need to remove it to match the address with the geonorge results
                        tmp = (addressSplit[1].Length == 5 && int.TryParse(addressSplit[1].Substring(1, 4), out _)) ? addressSplit[0] : matrikkelAddress;
                    }
                    else
                    {
                        tmp = matrikkelAddress;
                    }

                    if (tmp != string.Empty)
                    {
                        var address = addresses.Adresser?.SingleOrDefault(x => x.Adressetekst == tmp);

                        property.Address = address?.Adressetekst ?? string.Empty;
                        property.PostalCode = address?.Postnummer ?? string.Empty;
                        property.City = address?.Poststed ?? string.Empty;
                        property.MunicipalityNumber = address?.Kommunenummer ?? string.Empty;
                        property.Municipality = address?.Kommunenavn ?? string.Empty;
                    }
                }
                else
                {
                    OutputAdresse address = addresses?.Adresser?.FirstOrDefault();
                    if (address != null)
                    {
                        property.Address = address.Adressetekst;
                        property.PostalCode = address.Postnummer;
                        property.City = address.Poststed;
                        property.MunicipalityNumber = address.Kommunenummer;
                        property.Municipality = address.Kommunenavn;
                    }
                }
            }

            private static List<List<List<double>>> NormalizeCoordinates(JsonElement coords)
            {
                if (coords.ValueKind != JsonValueKind.Array || coords.GetArrayLength() == 0)
                    return new List<List<List<double>>>();

                var first = coords[0];

                // CASE 1: [x, y]  (rare, but possible)
                if (first.ValueKind == JsonValueKind.Number)
                {
                    var point = coords.EnumerateArray()
                        .Select(v => v.GetDouble())
                        .ToList();

                    return new List<List<List<double>>>
        {
            new List<List<double>> { point }
        };
                }

                // CASE 2: [[x,y], [x,y]]  (omrade=false)
                if (first.ValueKind == JsonValueKind.Array)
                {
                    var second = first[0];

                    // [[x,y]]
                    if (second.ValueKind == JsonValueKind.Number)
                    {
                        var ring = coords.EnumerateArray()
                            .Select(pair => pair.EnumerateArray()
                                .Select(v => v.GetDouble())
                                .ToList())
                            .ToList();

                        return new List<List<List<double>>> { ring };
                    }

                    // [[[x,y]]] (Polygon)
                    if (second.ValueKind == JsonValueKind.Array)
                    {
                        return coords.EnumerateArray()
                            .Select(ring => ring.EnumerateArray()
                                .Select(pair => pair.EnumerateArray()
                                    .Select(v => v.GetDouble())
                                    .ToList())
                                .ToList())
                            .ToList();
                    }
                }

                throw new NotSupportedException("Unsupported coordinate structure");
            }

        }
    }

}
