using Altinn.App.ExternalApi.AddressLookup;
using Dan.Common.Exceptions;
using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using Kartverket.Matrikkel.MatrikkelenhetService;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Matrikkel;

namespace Dan.Plugin.Kartverket.Clients
{
    public interface IAddressLookupClient
    {
        public Task<KartverketResponse> Get(KartverketResponse kartverket);
    }


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

        private async Task<OutputAdresseList> SokAdresse(Property property)
        {
            HttpResponseMessage response = null;

            var urlBuilder = new StringBuilder();
            urlBuilder.Append(_settings.AddressLookupUrl).Append("/sok?");

            if (string.IsNullOrEmpty(property.MunicipalityNumber) || string.IsNullOrEmpty(property.SubholdingNumber) || string.IsNullOrEmpty(property.HoldingNumber))
            {
                urlBuilder.Append("kommunenummer=").Append(int.Parse(property.MunicipalityNumber).ToString("d4")).Append('&');
                urlBuilder.Append("adressetekst=").Append(property.Address).Append('&');
            }
            else
            {
                urlBuilder.Append("kommunenummer=").Append(int.Parse(property.MunicipalityNumber).ToString("d4")).Append('&');
                urlBuilder.Append("bruksnummer=").Append(property.SubholdingNumber).Append('&');
                urlBuilder.Append("gardsnummer=").Append(property.HoldingNumber).Append('&');

                if (!string.IsNullOrEmpty(property.LeaseNumber))
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
                var address = addresses.Adresser?.SingleOrDefault(x => x.Adressetekst == matrikkelAddress);
                
                property.Address = address.Adressetekst;
                property.PostalCode = address.Postnummer;
                property.City = address.Poststed;
                property.MunicipalityNumber = address.Kommunenummer;
                property.Municipality = address.Kommunenavn;                
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
    }
}
