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

        public Task<AdresseResponse> FindAdresseForBorettslagsandel(string orgNo, int? shareNo);
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

        public async Task<AdresseResponse> FindAdresseForBorettslagsandel(string orgNo, int? shareNo)
        {
            return await MakeRequest<AdresseResponse>(_settings.KartverketAdresseForBorettslagsandelUrl, "",orgNo, shareNo);
        }

        private async Task<T> MakeRequest<T>(string url, string ssn, string orgNo = "", int? shareNo = 0) where T : new()
        {
            HttpResponseMessage response = null;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                if (!string.IsNullOrEmpty(ssn))
                {
                    request.Headers.TryAddWithoutValidation("identifikasjonsnummer", ssn);
                } else
                {
                    request.Headers.TryAddWithoutValidation("organisasjonsnummer", orgNo);
                    request.Headers.TryAddWithoutValidation("andelsnummer", shareNo.ToString());
                }

                response = await _httpClient.SendAsync(request);

                var responseData = await response.Content.ReadAsStringAsync();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    {
                        var res = JsonConvert.DeserializeObject<T>(responseData);
                        return res;
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
