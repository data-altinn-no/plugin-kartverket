using Dan.Plugin.Kartverket.Config;
using Kartverket.Grunnbok.RegisterenhetsrettService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class RegisterenhetsrettClientService : IRegisterenhetsrettClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;
        private IRequestContextService _requestContextService;

        private RegisterenhetsrettServiceClient _client;

        public RegisterenhetsrettClientService(ILoggerFactory factory, IOptions<ApplicationSettings> settings, IRequestContextService requestContextService)
        {
            _logger = factory.CreateLogger<RegisterenhetsrettClientService>();
            _settings = settings.Value;
            _requestContextService = requestContextService;

            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            _client = new RegisterenhetsrettServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokRootUrl + "/RegisterenhetsrettServiceWS"));
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
       
        private GrunnbokContext GetContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext,Timestamp>(_requestContextService.ServiceContext);
        }
    }

    public interface IRegisterenhetsrettClientService
    {
        public Task<RegisterenhetIdTilRegisterenhetsrettIdsMap> GetRetterForEnheter(string registerenhetsid);
    }
}
