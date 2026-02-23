using Dan.Common;
using Dan.Common.Extensions;
using Dan.Common.Models;
using Dan.Common.Util;
using Dan.Plugin.Kartverket.Clients;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceScopeFactory _scopeFactory;

        public Main(ILoggerFactory loggerFactory, IDDWrapper ddWrapper, IKartverketGrunnbokMatrikkelService kartverketGrunnbokMatrikkelService, IServiceScopeFactory scopeFactory)
        {
            _logger = loggerFactory.CreateLogger<Main>();
            _ddWrapper = ddWrapper;
            _kartverketGrunnbokMatrikkelService = kartverketGrunnbokMatrikkelService;
            _scopeFactory = scopeFactory;

        }

        [Function("Grunnbok")]
        public async Task<HttpResponseData> Grunnbok(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("Running func 'Grunnbok'");
            using var scope = _scopeFactory.CreateScope();
            var requestContextService = scope.ServiceProvider.GetRequiredService<IRequestContextService>();
            var diHeWrapper = scope.ServiceProvider.GetRequiredService<IDiHeWrapper>();

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            await requestContextService.SetRequestContext(evidenceHarvesterRequest.ServiceContext);

            return await EvidenceSourceResponse.CreateResponse(req, () => GetEvidenceValuesGrunnbok(evidenceHarvesterRequest));
        }

        [Function("GrunnbokRettigheter")]
        public async Task<HttpResponseData> GrunnbokRettigheter(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
    FunctionContext context)
        {
            _logger.LogInformation("Running func 'Grunnbok'");
            using var scope = _scopeFactory.CreateScope();
            var requestContextService = scope.ServiceProvider.GetRequiredService<IRequestContextService>();
            var diHeWrapper = scope.ServiceProvider.GetRequiredService<IDiHeWrapper>();

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            await requestContextService.SetRequestContext(evidenceHarvesterRequest.ServiceContext);

            return await EvidenceSourceResponse.CreateResponse(req, () => GetEvidenceValuesGrunnbokRettigheter(evidenceHarvesterRequest));
        }

        [Function("Eiendomsadresser")]
        public async Task<HttpResponseData> Eiendomsadresser(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req, FunctionContext context)
        {
            _logger.LogInformation("Running func 'Eiendomsadresser'");
            using var scope = _scopeFactory.CreateScope();
            var requestContextService = scope.ServiceProvider.GetRequiredService<IRequestContextService>();
            var diHeWrapper = scope.ServiceProvider.GetRequiredService<IDiHeWrapper>();

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            await requestContextService.SetRequestContext(evidenceHarvesterRequest.ServiceContext);

            return await EvidenceSourceResponse.CreateResponse(req, () => GetEvidenceValuesEiendomsadresser(evidenceHarvesterRequest));
        }

        [Function("Eiendommer")]
        public async Task<HttpResponseData> Eiendommer(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("Running func 'Eiendommer'");
            using var scope = _scopeFactory.CreateScope();
            var requestContextService = scope.ServiceProvider.GetRequiredService<IRequestContextService>();
            var diHeWrapper = scope.ServiceProvider.GetRequiredService<IDiHeWrapper>();

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            await requestContextService.SetRequestContext(evidenceHarvesterRequest.ServiceContext);

            return await EvidenceSourceResponse.CreateResponse(req, () => GetEvidenceValuesEiendommer(evidenceHarvesterRequest));
        }

        [Function("MotorisertFerdsel")]
        public async Task<HttpResponseData> MotorisertFerdsel([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,FunctionContext context)
        {
            _logger.LogInformation("Running func 'MotorisertFerdsel'");
            using var scope = _scopeFactory.CreateScope();

            var requestContextService = scope.ServiceProvider.GetRequiredService<IRequestContextService>();
            var diHeWrapper = scope.ServiceProvider.GetRequiredService<IDiHeWrapper>();

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var evidenceHarvesterRequest = JsonConvert.DeserializeObject<EvidenceHarvesterRequest>(requestBody);

            await requestContextService.SetRequestContext(evidenceHarvesterRequest.ServiceContext);

            return await EvidenceSourceResponse.CreateResponse(
                req,
                () => GetEvidenceValuesMotorisertFerdsel(evidenceHarvesterRequest, diHeWrapper)
            );
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesMotorisertFerdsel(
            EvidenceHarvesterRequest evidenceHarvesterRequest,
            IDiHeWrapper diheWrapper)
        {
            var result = await diheWrapper.GetMotorizedTrafficInformation(evidenceHarvesterRequest.SubjectParty.GetAsString(false));

            var ecb = new EvidenceBuilder(new Metadata(), "MotorisertFerdsel");
            ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(result), Metadata.SOURCE, false);
            return ecb.GetEvidenceValues();
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
                if (!evidenceHarvesterRequest.TryGetParameter("Enkeltadresse", out bool single))
                {
                    single = false;
                }

                var ecb = new EvidenceBuilder(new Metadata(), "Grunnbok");
                var result = await _ddWrapper.GetDDGrunnbok(evidenceHarvesterRequest.SubjectParty.NorwegianSocialSecurityNumber, true, single);
                ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(result), Metadata.SOURCE, false);

                return ecb.GetEvidenceValues();
            }
            catch (Exception e)
            {
                _logger.LogError($"Func 'Grunnbok' failed for input '{(evidenceHarvesterRequest.SubjectParty.GetAsString())}': {e.Message}");

                throw;
            }
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesGrunnbokRettigheter(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            try
            {
                var ecb = new EvidenceBuilder(new Metadata(), "Grunnbok");
                var result = await _ddWrapper.GetDDGrunnbok(evidenceHarvesterRequest.SubjectParty.NorwegianSocialSecurityNumber, false);
                ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(result), Metadata.SOURCE, false);

                return ecb.GetEvidenceValues();
            }
            catch (Exception e)
            {
                _logger.LogError($"Func 'Grunnbok' failed for input '{(evidenceHarvesterRequest.SubjectParty.GetAsString())}': {e.Message}");

                throw;
            }
        }

        private async Task<List<EvidenceValue>> GetEvidenceValuesEiendomsadresser(EvidenceHarvesterRequest evidenceHarvesterRequest)
        {
            try
            {
                var ecb = new EvidenceBuilder(new Metadata(), "Eiendomsadresser");

                evidenceHarvesterRequest.TryGetParameter("Gnr", out int gnr);
                evidenceHarvesterRequest.TryGetParameter("Bnr", out int bnr);
                evidenceHarvesterRequest.TryGetParameter("Fnr", out int fnr);
                evidenceHarvesterRequest.TryGetParameter("Snr", out int snr);
                evidenceHarvesterRequest.TryGetParameter("Knr", out string knr);
                evidenceHarvesterRequest.TryGetParameter("Enkeltadresse", out bool single);

                var result = await _ddWrapper.GetDDAdresser(gnr, bnr, fnr, snr, knr, single);

                ecb.AddEvidenceValue("default", JsonConvert.SerializeObject(result), Metadata.SOURCE, false);

                return ecb.GetEvidenceValues();
            }
            catch (Exception e)
            {
                _logger.LogError($"Func 'Eiendomsadresser' failed: {e.Message}");

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
