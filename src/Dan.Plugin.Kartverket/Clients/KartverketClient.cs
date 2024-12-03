using Dan.Common.Exceptions;
using Dan.Plugin.Kartverket;
using Dan.Plugin.Kartverket.Config;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Kartverket.Clients;

namespace Dan.Plugin.Kartverket.Clients
{

    public interface IKartverketClient
    {
        //public Task<KartverketResponse> Get(string ssn);
        public Task<RegisterenhetsrettsandelerResponse> FindRegisterenhetsrettsandelerForPerson(string ssn);
        public Task<RettigheterResponse> FindRettigheterForPerson(string ssn);
    }
    public class KartverketClient : IKartverketClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationSettings _settings;

        public KartverketClient(
            HttpClient httpClient,
            IOptions<ApplicationSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<RegisterenhetsrettsandelerResponse> FindRegisterenhetsrettsandelerForPerson(string ssn)
        {
            return await MakeRequest<RegisterenhetsrettsandelerResponse>(_settings.KartverketRegisterenhetsrettsandelerForPersonUrl, ssn);
        }

        public async Task<RettigheterResponse> FindRettigheterForPerson(string ssn)
        {
            return await MakeRequest<RettigheterResponse>(_settings.KartverketRettigheterForPersonUrl, ssn);
        }

        private async Task<T> MakeRequest<T>(string url, string ssn) where T : new()
        {
            HttpResponseMessage response = null;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("identifikasjonsnummer", ssn);
                response = await _httpClient.SendAsync(request);

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
    }
}
