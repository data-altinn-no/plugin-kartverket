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
            GrunnbokHelpers.SetCredentials(_client.ClientCredentials, _settings, ServiceContext.Grunnbok);  
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
       
        private GrunnbokContext GetContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext,Timestamp>();
        }
    }

    public interface IRegisterenhetsrettClientService
    {
        public Task<RegisterenhetIdTilRegisterenhetsrettIdsMap> GetRetterForEnheter(string registerenhetsid);
    }
}
