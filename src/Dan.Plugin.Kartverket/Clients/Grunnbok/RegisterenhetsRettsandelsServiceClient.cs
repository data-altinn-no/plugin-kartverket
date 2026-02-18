using Dan.Plugin.Kartverket.Config;
using Kartverket.Grunnbok.RegisterenhetsrettsandelService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class RegisterenhetsRettsandelsServiceClientService : IRegisterenhetsRettsandelsServiceClientService
    {
        private readonly ILogger _logger;
        private readonly ApplicationSettings _settings;
        private readonly IRequestContextService _requestContextService;

        public RegisterenhetsRettsandelsServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<RegisterenhetsRettsandelsServiceClientService>();
            _requestContextService = requestContextService;
        }

        public async Task<List<string>> GetAndelerForRettighetshaver(string personident)
        {
            var result = new List<string>();
            var client = CreateClient();

            var request = new findAndelerForRettighetshavereRequest()
            {
                Body = new findAndelerForRettighetshavereRequestBody()
                {
                    personIds = new PersonIdList()
                    {
                        new()
                        {
                            value = personident
                        }
                    },
                    grunnbokContext = GetContext()
                }
            };

            try
            {
                var rettighetsresponse = await client.findAndelerForRettighetshavereAsync(request);
                var retter = rettighetsresponse.Body.@return.Values.ToList();

                if(retter.Count > 0)
                    result.AddRange(retter[0].Select(x => x.value));
            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                try { await client.CloseAsync(); }
                catch { client.Abort(); }
            }

            return result;
        }

        public async Task<findAndelerIRetterResponse> GetAndelerIRetter(string registerenhetsid)
        {
            var result = new findAndelerIRetterResponse();
            var client = CreateClient();

            try
            {
                var request = new findAndelerIRetterRequest
                {
                    Body = new findAndelerIRetterRequestBody
                    {
                        grunnbokContext = GetContext(),
                        rettIds = new RegisterenhetsrettIdList
                        {
                            new RegisterenhetsrettId
                            {
                                value = registerenhetsid
                            }
                        }
                    }
                };

                var response = await client.findAndelerIRetterAsync(request);
                result = response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception was thrown while calling findAndelerIRetter");
            }
            finally
            {
                try { await client.CloseAsync(); }
                catch { client.Abort(); }
            }

            return result;
        }

        public async Task<findAndelerIRetterResponse> GetAndelerIRetter(string registerenhetsid)
        {
            var result = new findAndelerIRetterResponse();
            var client = CreateClient();

            try
            {
                var request = new findAndelerIRetterRequest
                {
                    Body = new findAndelerIRetterRequestBody
                    {
                        grunnbokContext = GetContext(),
                        rettIds = new RegisterenhetsrettIdList
                        {
                            new RegisterenhetsrettId
                            {
                                value = registerenhetsid
                            }
                        }
                    }
                };

                var response = await client.findAndelerIRetterAsync(request);
                result = response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception was thrown while calling findAndelerIRetter");
            }
            finally
            {
                try { await client.CloseAsync(); }
                catch { client.Abort(); }
            }

            return result;
        }

        private GrunnbokContext GetContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext, Timestamp>(_requestContextService.ServiceContext);
        }

        private RegisterenhetsrettsandelServiceClient CreateClient()
        {
            var serviceContext = _requestContextService.ServiceContext;

            if (string.IsNullOrWhiteSpace(serviceContext))
                throw new InvalidOperationException(
                    "ServiceContext is not set. Ensure SetRequestContext() is called before using RegisterenhetsRettsandelsServiceClientService.");

            var binding = GrunnbokHelpers.GetBasicHttpBinding();

            var endpoint = new EndpointAddress(
                $"{_settings.GrunnbokRootUrl}RegisterenhetsrettsandelServiceWS");

            var client = new RegisterenhetsrettsandelServiceClient(binding, endpoint);

            GrunnbokHelpers.SetGrunnbokWSCredentials(
                client.ClientCredentials,
                _settings,
                serviceContext);

            return client;
        }

    }

    public interface IRegisterenhetsRettsandelsServiceClientService
    {
        public Task<List<string>> GetAndelerForRettighetshaver(string personident);
        public Task<findAndelerIRetterResponse> GetAndelerIRetter(string registerenhetsid);
    }
}
