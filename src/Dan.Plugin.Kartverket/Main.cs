using Dan.Common;
using Dan.Common.Models;
using Dan.Common.Util;
using Dan.Plugin.Kartverket.Clients;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket
{
    public class Main
    {
        private ILogger _logger;
        private IKartverketGrunnbokMatrikkelService _kartverketGrunnbokMatrikkelService;
        private readonly IDDWrapper _ddWrapper;


        public Main(ILoggerFactory loggerFactory, IDDWrapper ddWrapper, IKartverketGrunnbokMatrikkelService kartverketGrunnbokMatrikkelService)
        {
            _logger = loggerFactory.CreateLogger<Main>();
            _ddWrapper = ddWrapper;
            _kartverketGrunnbokMatrikkelService = kartverketGrunnbokMatrikkelService;
        }

        [Function("Grunnbok")]
        public async Task<HttpResponseData> Grunnbok(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("Running func 'Grunnbok'");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            return await EvidenceSourceResponse.CreateResponse(req, () => GetEvidenceValuesGrunnbok(evidenceHarvesterRequest));
        }

        [Function("Eiendommer")]
        public async Task<HttpResponseData> Eiendommer(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("Running func 'Eiendommer'");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            return await EvidenceSourceResponse.CreateResponse(req, () => GetEvidenceValuesEiendommer(evidenceHarvesterRequest));
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesEiendommer(EvidenceHarvesterRequest request)
        {
            var result = await _kartverketGrunnbokMatrikkelService.FindProperties(request.SubjectParty.NorwegianOrganizationNumber);

            var ecb = new EvidenceBuilder(new Metadata(), "Eiendommer");
            ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(result), Metadata.SOURCE, false);
            return ecb.GetEvidenceValues();
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesGrunnbok(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            try
            {
                var ecb = new EvidenceBuilder(new Metadata(), "Grunnbok");
                var result = await _ddWrapper.GetDDGrunnbok(evidenceHarvesterRequest.SubjectParty.NorwegianSocialSecurityNumber);
                ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(result), Metadata.SOURCE, false);

                return ecb.GetEvidenceValues();
            }
            catch (Exception e)
            {
                _logger.LogError($"Func 'Grunnbok' failed for input '{(evidenceHarvesterRequest.SubjectParty.GetAsString())}': {e.Message}");

                throw;
            }
        }

        [Function(Constants.EvidenceSourceMetadataFunctionName)]
        public async Task<HttpResponseData> GetMetadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation($"Running func metadata for {Constants.EvidenceSourceMetadataFunctionName}");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new Metadata().GetEvidenceCodes());

            return response;
        }
    }
}
