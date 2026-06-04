using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces;
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
        private readonly ApplicationSettings _settings;
        private readonly ILogger _logger;
        private readonly IRequestContextService _requestContextService;

        public RegisterenhetsrettClientService(ILoggerFactory factory, IOptions<ApplicationSettings> settings, IRequestContextService requestContextService)
        {
            _logger = factory.CreateLogger<RegisterenhetsrettClientService>();
            _settings = settings.Value;
            _requestContextService = requestContextService;
        }

        public async Task<RegisterenhetIdTilRegisterenhetsrettIdsMap> GetRetterForEnheter(string registerenhetsid)
        {
            var client = CreateClient();
            RegisterenhetIdTilRegisterenhetsrettIdsMap result = new();
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
                await ((IClientChannel)client).CloseChannelAsync();
            }

            return result;
        }
       
        private GrunnbokContext GetContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext,Timestamp>(_requestContextService.ServiceContext);
        }

        private RegisterenhetsrettService CreateClient()
        {
            var serviceContext = _requestContextService.ServiceContext;

            if (string.IsNullOrWhiteSpace(serviceContext))
            {
                throw new InvalidOperationException(
                    "ServiceContext is not set. Ensure SetRequestContext() is called before using RegisterenhetsrettClientService.");
            }

            var endpointAddress = $"{_settings.GrunnbokRootUrl}RegisterenhetsrettServiceWS";

            return WcfChannelFactoryCache<RegisterenhetsrettService>.CreateChannel(
                $"{endpointAddress}|{serviceContext}",
                new EndpointAddress(endpointAddress),
                GrunnbokHelpers.GetBasicHttpBinding(),
                credentials => GrunnbokHelpers.SetGrunnbokWSCredentials(credentials, _settings, serviceContext));
        }

    }
}
