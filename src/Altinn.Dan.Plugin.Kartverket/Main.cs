using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Kartverket.Clients;
using Altinn.Dan.Plugin.Kartverket.Config;
using Azure.Core.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nadobe;
using Nadobe.Common.Models;
using Nadobe.Common.Util;
using Newtonsoft.Json;

namespace Altinn.Dan.Plugin.Kartverket
{
    public class Main
    {
        private ILogger _logger;
        private readonly ApplicationSettings _settings;
        private readonly KartverketClient _kartverketClient;

        public Main(IOptions<ApplicationSettings> settings, KartverketClient kartverketClient)
        {
            _settings = settings.Value;
            _kartverketClient = kartverketClient;
        }

        [Function("Grunnbok")]
        public async Task<HttpResponseData> Grunnbok(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger = context.GetLogger(context.FunctionDefinition.Name);
            _logger.LogInformation("Running func 'Grunnbok'");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            var actionResult = await EvidenceSourceResponse.CreateResponse(null, () => GetEvidenceValuesGrunnbok(evidenceHarvesterRequest)) as ObjectResult;
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(actionResult?.Value);

            return response;
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesGrunnbok(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            var subject = evidenceHarvesterRequest.SubjectParty;
            try
            {
                var ecb = new EvidenceBuilder(new Metadata(), "Grunnbok");
                ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(await _kartverketClient.Get(subject?.NorwegianSocialSecurityNumber)), Metadata.SOURCE);

                return ecb.GetEvidenceValues();
            }
            catch (Exception e)
            {
                _logger.LogError($"Func 'Grunnbok' failed for input '{(subject?.NorwegianSocialSecurityNumber.Length < 6 ? subject.NorwegianSocialSecurityNumber : subject?.GetAsString())}': {e.Message}");

                throw;
            }
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
