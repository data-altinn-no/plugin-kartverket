using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.DATASOURCENAME.Config;
using Azure.Core.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nadobe;
using Nadobe.Common.Exceptions;
using Nadobe.Common.Models;
using Nadobe.Common.Util;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.DATASOURCENAME
{
    public class Main
    {
        private ILogger _logger;
        private readonly HttpClient _client;
        private readonly ApplicationSettings _settings;

        public Main(IHttpClientFactory httpClientFactory, IOptions<ApplicationSettings> settings)
        {
            _client = httpClientFactory.CreateClient("SafeHttpClient");
            _settings = settings.Value;
        }

        [Function("DATASETNAME1")]
        public async Task<HttpResponseData> Dataset1(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            _logger.LogInformation("Running func 'DATASETNAME1'");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            var actionResult = await EvidenceSourceResponse.CreateResponse(null, () => GetEvidenceValuesDatasetName1(evidenceHarvesterRequest)) as ObjectResult;
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(actionResult?.Value);

            return response;
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesDatasetName1(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            var content = await MakeRequest(string.Format(_settings.DATASETNAME1URL, evidenceHarvesterRequest.OrganizationNumber), evidenceHarvesterRequest.OrganizationNumber);

            var ecb = new EvidenceBuilder(new Metadata(), "DATASETNAME1");
            ecb.AddEvidenceValue($"field1", content.responsefield1, Metadata.SOURCE);
            ecb.AddEvidenceValue($"field2", content.responsefield2, Metadata.SOURCE);

            return ecb.GetEvidenceValues();
        }

        private async Task<dynamic> MakeRequest(string target, string organizationNumber)
        {
            HttpResponseMessage result = null;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, target);
                result = await _client.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                throw new EvidenceSourcePermanentServerException(Metadata.ERROR_CCR_UPSTREAM_ERROR, null, ex);
            }

            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                throw new EvidenceSourcePermanentClientException(Metadata.ERROR_ORGANIZATION_NOT_FOUND, $"{organizationNumber} could not be found");
            }

            var response = JsonConvert.DeserializeObject(await result.Content.ReadAsStringAsync());
            if (response == null)
            {
                throw new EvidenceSourcePermanentServerException(Metadata.ERROR_CCR_UPSTREAM_ERROR,
                    "Did not understand the data model returned from upstream source");
            }

            return response;
        }

        [Function("DATASETNAME2")]
        public async Task<HttpResponseData> Dataset2(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            _logger.LogInformation("Running func 'DATASETNAME2'");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            var actionResult = await EvidenceSourceResponse.CreateResponse(null, () => GetEvidenceValuesDatasetName2(evidenceHarvesterRequest)) as ObjectResult;
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(actionResult?.Value);

            return response;
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesDatasetName2(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            dynamic content = await MakeRequest(string.Format(_settings.DATASETNAME2URL, evidenceHarvesterRequest.OrganizationNumber), evidenceHarvesterRequest.OrganizationNumber);

            var ecb = new EvidenceBuilder(new Metadata(), "DATASETNAME2");
            ecb.AddEvidenceValue($"field1", content.responsefield1, Metadata.SOURCE);
            ecb.AddEvidenceValue($"field2", content.responsefield2, Metadata.SOURCE);
            ecb.AddEvidenceValue($"field3", content.responsefield3, Metadata.SOURCE);

            return ecb.GetEvidenceValues();
        }

        [Function(Constants.EvidenceSourceMetadataFunctionName)]
        public async Task<HttpResponseData> GetMetadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            _logger.LogInformation($"Running func metadata for {Constants.EvidenceSourceMetadataFunctionName}");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new Metadata().GetEvidenceCodes(),
                new NewtonsoftJsonObjectSerializer(new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto }));

            return response;
        }
    }
}
