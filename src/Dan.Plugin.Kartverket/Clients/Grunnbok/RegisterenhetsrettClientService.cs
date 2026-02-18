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
        private IRequestContextService _requestContextService;

        public RegisterenhetsrettClientService(ILoggerFactory factory, IOptions<ApplicationSettings> settings, IRequestContextService requestContextService)
        {
            _logger = factory.CreateLogger<RegisterenhetsrettClientService>();
            _settings = settings.Value;
            _requestContextService = requestContextService;
        }

        public async Task<RegisterenhetIdTilRegisterenhetsrettIdsMap> GetRetterForEnheter(string registerenhetsid)
        {
            var client = CreateClient();
            RegisterenhetIdTilRegisterenhetsrettIdsMap result = null;
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
            try
            {
                var response = await client.findRetterForEnheterAsync(request);
                result = response.Body.@return;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error calling findRetterForEnheterAsync for registerenhetsid {Registerenhetsid}", registerenhetsid);
            }
            finally
            {
                try { client.Close(); }
                catch { client.Abort(); }
            }

            return result;
        }
       
        private GrunnbokContext GetContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext,Timestamp>(_requestContextService.ServiceContext);
        }

        private RegisterenhetsrettServiceClient CreateClient()
        {
            var serviceContext = _requestContextService.ServiceContext;

            if (string.IsNullOrWhiteSpace(serviceContext))
            {
                throw new InvalidOperationException(
                    "ServiceContext is not set. Ensure SetRequestContext() is called before using RegisterenhetsrettClientService.");
            }

            var binding = GrunnbokHelpers.GetBasicHttpBinding();

            var endpoint = new EndpointAddress(
                $"{_settings.GrunnbokRootUrl}RegisterenhetsrettServiceWS");

            var client = new RegisterenhetsrettServiceClient(binding, endpoint);

            GrunnbokHelpers.SetGrunnbokWSCredentials(
                client.ClientCredentials,
                _settings,
                serviceContext);

            return client;
        }

    }

    public interface IRegisterenhetsrettClientService
    {
        public Task<RegisterenhetIdTilRegisterenhetsrettIdsMap> GetRetterForEnheter(string registerenhetsid);
    }
}
