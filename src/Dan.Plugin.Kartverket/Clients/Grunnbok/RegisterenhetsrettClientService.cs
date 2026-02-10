using Dan.Plugin.Kartverket.Config;
using Kartverket.Grunnbok.RegisterenhetsrettService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class RegisterenhetsrettClientService : IRegisterenhetsrettClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;

        private RegisterenhetsrettServiceClient _client;

        public RegisterenhetsrettClientService(ILoggerFactory factory, IOptions<ApplicationSettings> settings)
        {
            _logger = factory.CreateLogger<RegisterenhetsrettClientService>();
            _settings = settings.Value;

            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();
            _client = new RegisterenhetsrettServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokBaseUrl + "/RegisterenhetsrettServiceWS"));
            _client.ClientCredentials.UserName.UserName = _settings.GrunnbokUser2;
            _client.ClientCredentials.UserName.Password = _settings.GrunnbokPw2;
            GrunnbokHelpers.SetGrunnbokWSCredentials(_client.ClientCredentials, _settings);  
        }

        public async Task<RegisterenhetIdTilRegisterenhetsrettIdsMap> GetRetterForEnheter(string registerenhetsid)
        {
            var request = new findRetterForEnheterRequest()
            {
                Body = new findRetterForEnheterRequestBody()
                {
                    grunnbokContext = GetContext(),
                    registerenhetIds = new RegisterenhetIdList()
                    {
                        new RegisterenhetId()
                        {
                            value = registerenhetsid
                        }
                    }
                }
            };

            var response = await _client.findRetterForEnheterAsync(request);

            return response.Body.@return;
        }

        
        public async Task<findRetterForEnheterOgTyperResponse> GetRetterForEnheterOgTyper(string registerenhetsid)
        {
            try
            {
                var request = new findRetterForEnheterOgTyperRequest()
                {
                    Body = new findRetterForEnheterOgTyperRequestBody()
                    {
                        grunnbokContext = GetContext(),
                        registerenhetIds = new RegisterenhetIdList()
                        {
                            new RegisterenhetId()
                            {
                                value = registerenhetsid
                            }
                        }
                    }
                };

                var response = await _client.findRetterForEnheterOgTyperAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Exception was thrown: {ex}");
            }

            return null;
        }
        
        private GrunnbokContext GetContext()
        {
            return new GrunnbokContext()
            {
                clientIdentification = "eDueDiligence",
                clientTraceInfo = "eDueDiligence_1",
                locale = "no_578",
                snapshotVersion = new()
                {
                    timestamp = new DateTime(9999, 1, 1, 0, 0, 0)
                },
                systemVersion = "1"
            };
        }
    }

    public interface IRegisterenhetsrettClientService
    {
        public Task<RegisterenhetIdTilRegisterenhetsrettIdsMap> GetRetterForEnheter(string registerenhetsid);
        public Task<findRetterForEnheterOgTyperResponse> GetRetterForEnheterOgTyper(string registerenhetsid);
    }
}
