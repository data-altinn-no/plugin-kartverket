using Altinn.App.ExternalApi.AddressLookup;
using Dan.Common.Exceptions;
using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
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

        public AddressLookupClient(IHttpClientFactory httpClientFactory, IOptions<ApplicationSettings> settings)
        {
            _httpClient = httpClientFactory.CreateClient("SafeHttpClient");
            _settings = settings.Value;
        }

        public async Task<KartverketResponse> Get(KartverketResponse kartverket)
        {
            List<Property> properties = new List<Property>();
            foreach (Property property in kartverket.PropertyRights.Properties)
            {
                properties.Add(property);
                UpdateProperty(property, await SokAdresse(property));
            }

            kartverket.PropertyRights.Properties = properties;

            List<PropertyWithRights> propertyWithRights = new List<PropertyWithRights>();
            foreach (PropertyWithRights property in kartverket.PropertyRights.PropertiesWithRights)
            {
                propertyWithRights.Add(property);
                UpdateProperty(property, await SokAdresse(property));
            }

            kartverket.PropertyRights.PropertiesWithRights = propertyWithRights;

            return kartverket;
        }

        private async Task<OutputAdresseList> SokAdresse(Property property)
        {
            if (string.IsNullOrEmpty(property.MunicipalityNumber) || string.IsNullOrEmpty(property.SubholdingNumber) || string.IsNullOrEmpty(property.HoldingNumber))
            {
                return null;
            }

            HttpResponseMessage response = null;
            try
            {
                var urlBuilder = new StringBuilder();
                urlBuilder.Append(_settings.AddressLookupUrl).Append("/sok?");
                urlBuilder.Append("kommunenummer=").Append(int.Parse(property.MunicipalityNumber).ToString("d4")).Append('&');
                urlBuilder.Append("bruksnummer=").Append(property.SubholdingNumber).Append('&');
                urlBuilder.Append("gardsnummer=").Append(property.HoldingNumber).Append('&');
                if (!string.IsNullOrEmpty(property.LeaseNumber))
                {
                    urlBuilder.Append("festenummer=").Append(Uri.EscapeDataString(property.LeaseNumber)).Append('&');
                }

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

        private void UpdateProperty(Property property, OutputAdresseList addresses)
        {
            //TODO: Should this use 'FirstOrDefault'? (ex. Myrdalsvegen)
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
