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

        public IdentServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<IdentServiceClientService>();
        }

        public async Task<string> GetPersonIdentity(string personId)
        {
            //Find ident for identifier
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();
            string identity = string.Empty;

            var identService = new IdentServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokRootUrl + "IdentServiceWS"));
            GrunnbokHelpers.SetGrunnbokWSCredentials(identService.ClientCredentials, _settings);

            var list = new PersonIdentList()
            {
                new ()
                {
                    identifikasjonsnummer = personId
                }
            };

            var grunnbokContext = new GrunnbokContext()
            {
                locale = "no_578",
                clientIdentification = "eDueDiligence",
                clientTraceInfo = "eDueDiligence_1",
                systemVersion = "1",
                snapshotVersion = new global::Kartverket.Grunnbok.IdentService.Timestamp()
                {
                    timestamp = new DateTime(9999, 1, 1, 0, 0, 0)
                }
            };

            var request = new findPersonIdsForIdentsRequest()
            {
                Body = new findPersonIdsForIdentsRequestBody()
                {
                    grunnbokContext = grunnbokContext,
                    idents = list
                }
            };

            try
            {
                var identResponse = await identService.findPersonIdsForIdentsAsync(request);
                identity = identResponse.Body.@return.Values.FirstOrDefault().value;
                
            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return identity;

            
        }
    }

    public interface IIdentServiceClientService
    {
        public Task<string> GetPersonIdentity(string personId);
    }
}
