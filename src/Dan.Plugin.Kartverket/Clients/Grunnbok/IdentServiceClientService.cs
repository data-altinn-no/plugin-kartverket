using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Grunnbok.IdentService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class IdentServiceClientService : IIdentServiceClientService
    {
        private readonly ApplicationSettings _settings;
        private readonly ILogger _logger;
        private readonly IRequestContextService _requestContextService;
        public IdentServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<IdentServiceClientService>();
            _requestContextService = requestContextService;
        }

        public async Task<string> GetPersonIdentity(string personId)
        {
            //Find ident for identifier
            string identity = string.Empty;
            var identService = CreateClient();

            var list = new PersonIdentList()
            {
                new ()
                {
                    identifikasjonsnummer = personId
                }
            };
            
            var request = new findPersonIdsForIdentsRequest()
            {
                Body = new findPersonIdsForIdentsRequestBody()
                {
                    grunnbokContext = GetContext(),
                    idents = list
                }
            };

            try
            {
                var identResponse = await identService.findPersonIdsForIdentsAsync(request);
                identity = identResponse.Body.@return.Values.FirstOrDefault().value;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling findPersonIdsForIdentsAsync for personId {PersonId}", personId);
            }
            finally
            {
                await ((IClientChannel)identService).CloseChannelAsync();
            }

            return identity;
        }

        private GrunnbokContext GetContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext, Timestamp>(_requestContextService.ServiceContext);
        }

        private IdentService CreateClient()
        {
            var serviceContext = _requestContextService.ServiceContext;

            if (string.IsNullOrWhiteSpace(serviceContext))
            {
                throw new InvalidOperationException(
                    "ServiceContext is not set. Ensure SetRequestContext() is called before using IdentServiceClientService.");
            }

            var endpointAddress = _settings.GrunnbokRootUrl + "IdentServiceWS";

            return WcfChannelFactoryCache<IdentService>.CreateChannel(
                $"{endpointAddress}|{serviceContext}",
                new EndpointAddress(endpointAddress),
                GrunnbokHelpers.GetBasicHttpBinding(),
                credentials => GrunnbokHelpers.SetGrunnbokWSCredentials(credentials, _settings, serviceContext));
        }

    }
}
