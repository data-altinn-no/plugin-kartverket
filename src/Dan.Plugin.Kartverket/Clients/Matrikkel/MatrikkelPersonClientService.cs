using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Matrikkel.PersonService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel
{
    public class MatrikkelPersonClientService : IMatrikkelPersonClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;
        private IRequestContextService _requestContextService;

        public MatrikkelPersonClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelPersonClientService>();            
            _requestContextService = requestContextService;
        }


        public async Task<long> GetOrganization(string orgno)
        {
            findPersonIdForIdentResponse result = null;
            var _client = CreateClient();

            var request = new findPersonIdForIdentRequest()
            {
                matrikkelContext = GetContext(),
                personIdent = new JuridiskPersonIdent()
                {
                    organisasjonsnummer = orgno
                }
            };

            try
            {
                result = await _client.findPersonIdForIdentAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result.@return.value;
        }

        public async Task<long> GetPerson(string nin)
        {
            findPersonIdForIdentResponse result = null;
            var _client = CreateClient();

            var request = new findPersonIdForIdentRequest()
            {
                matrikkelContext = GetContext(),
                personIdent = new FysiskPersonIdent()
                {
                    fodselsnummer = nin
                }
            };

            try
            {
                result = await _client.findPersonIdForIdentAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result.@return.value;
        }

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<MatrikkelContext, Timestamp>();
        }

        private PersonServiceClient CreateClient()
        {
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            var client = new PersonServiceClient(
                myBinding,
                new EndpointAddress(_settings.MatrikkelRootUrl + "PersonServiceWS")
            );

            GrunnbokHelpers.SetMatrikkelWSCredentials(
                client.ClientCredentials,
                _settings,
                _requestContextService.ServiceContext
            );

            return client;
        }
    }

    public interface IMatrikkelPersonClientService
    {
        Task<long> GetOrganization(string orgno);

        Task<long> GetPerson(string nin);

    }
}
