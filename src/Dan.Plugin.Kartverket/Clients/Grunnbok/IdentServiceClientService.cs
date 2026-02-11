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
            GrunnbokHelpers.SetCredentials(identService.ClientCredentials, _settings, ServiceContext.Grunnbok);

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
                    grunnbokContext = GetGrunnbokContext(),
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

        private GrunnbokContext GetGrunnbokContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext,Timestamp>();
        }
    }

        public interface IIdentServiceClientService
        {
            public Task<string> GetPersonIdentity(string personId);
        }
    }
