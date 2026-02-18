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
        private ApplicationSettings _settings;
        private ILogger _logger;
        private IRequestContextService _requestContextService;
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
                try { await identService.CloseAsync(); }
                catch { identService.Abort(); }
            }

            return identity;
        }

        private GrunnbokContext GetContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext, Timestamp>(_requestContextService.ServiceContext);
        }

        private IdentServiceClient CreateClient()
        {
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            if (string.IsNullOrWhiteSpace(_requestContextService.ServiceContext))
            {
                throw new InvalidOperationException(
                    "ServiceContext is not set. Ensure SetRequestContext() is called before using IdentServiceClientService.");
            }
            var client = new IdentServiceClient(
                myBinding,
                new EndpointAddress(_settings.GrunnbokRootUrl + "IdentServiceWS"));

            GrunnbokHelpers.SetGrunnbokWSCredentials(
                client.ClientCredentials,
                _settings,
                _requestContextService.ServiceContext);

            return client;


            

        }

    }

    public interface IIdentServiceClientService
        {
            public Task<string> GetPersonIdentity(string personId);
        }
    }
