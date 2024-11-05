using Altinn.Dan.Plugin.Kartverket.Clients;
using Dan.Common.Exceptions;
using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients
{
    public interface ILandbrukClient
    {
        public Task<KartverketResponse> Get(KartverketResponse kartverket);
    }

    public class LandbrukClient : ILandbrukClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationSettings _settings;

        public LandbrukClient(HttpClient httpClient, IOptions<ApplicationSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<KartverketResponse> Get(KartverketResponse kartverket)
        {
            return PopulateIsAgriculture(kartverket, await MatrikkelSoek(GetMatrikkelList(kartverket)));
        }

        private async Task<ICollection<MatrikkelRespons>> MatrikkelSoek(IEnumerable<MatrikkelNummer> matrikkelnummerList)
        {
            if (matrikkelnummerList.IsNullOrEmpty())
            {
                return null;
            }

            HttpResponseMessage response = null;
            try
            {
                var body = new StringContent(JsonConvert.SerializeObject(matrikkelnummerList));
                body.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                response = await _httpClient.PostAsync(_settings.LandbrukUrl, body);
                var responseData = await response.Content.ReadAsStringAsync();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    {
                        return JsonConvert.DeserializeObject<ICollection<MatrikkelRespons>>(responseData);
                    }
                    default:
                    {
                        throw new EvidenceSourcePermanentClientException(Metadata.ERROR_CCR_UPSTREAM_ERROR,
                            $"External API call to Landbruksdirektoratet failed ({(int)response.StatusCode} - {response.StatusCode})" + (string.IsNullOrEmpty(responseData) ? string.Empty : $", details: {responseData}"));
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

        private KartverketResponse PopulateIsAgriculture(KartverketResponse kartverket, ICollection<MatrikkelRespons> matrikkels)
        {
            if (matrikkels.IsNullOrEmpty())
            {
                return kartverket;
            }

            List<Property> properties = new List<Property>();
            foreach (Property property in kartverket.PropertyRights.Properties)
            {
                properties.Add(property);
                UpdateProperty(property, matrikkels);
            }

            kartverket.PropertyRights.Properties = properties;

            List<PropertyWithRights> propertyWithRights = new List<PropertyWithRights>();
            foreach (PropertyWithRights property in kartverket.PropertyRights.PropertiesWithRights)
            {
                propertyWithRights.Add(property);
                UpdateProperty(property, matrikkels);
            }

            kartverket.PropertyRights.PropertiesWithRights = propertyWithRights;

            return kartverket;
        }

        private void UpdateProperty(Property property, ICollection<MatrikkelRespons> matrikkels)
        {
            if (!(property.LeaseNumber != null && property.HoldingNumber != null && property.SubholdingNumber != null))
            {
                return;
            }

            if (matrikkels.ToList().Exists(m =>
                    m.MatrikkelNummer.Bruksnr == int.Parse(property.SubholdingNumber) &&
                    m.MatrikkelNummer.Festenr == int.Parse(property.LeaseNumber) &&
                    m.MatrikkelNummer.Gardsnr == int.Parse(property.HoldingNumber) &&
                    m.MatrikkelNummer.Kommunenr == property.MunicipalityNumber &&
                    m.TilknyttetLandbrukseiendom != null))
            {
                property.IsAgriculture = true;
            }
        }

        private List<MatrikkelNummer> GetMatrikkelList(KartverketResponse kartverket)
        {
            //TODO: Figure out: No 'p.Municipality != null'?
            return kartverket.PropertyRights.Properties.Union(kartverket.PropertyRights.PropertiesWithRights)
                .Where(p => p.LeaseNumber != null && p.HoldingNumber != null && p.SubholdingNumber != null)
                .Select(property => MapProperty(property))
                .ToList();
        }

        private MatrikkelNummer MapProperty(Property property)
        {
            return new MatrikkelNummer()
            {
                Bruksnr = int.Parse(property.SubholdingNumber),
                Festenr = int.Parse(property.LeaseNumber),
                Gardsnr = int.Parse(property.HoldingNumber),
                Kommunenr = property.MunicipalityNumber
            };
        }
    }
}
