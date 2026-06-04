using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Matrikkel.PersonService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel
{
    public class MatrikkelPersonClientService : IMatrikkelPersonClientService
    {
        private readonly ApplicationSettings _settings;
        private readonly ILogger _logger;
        private readonly IRequestContextService _requestContextService;

        public MatrikkelPersonClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelPersonClientService>();            
            _requestContextService = requestContextService;
        }

        public async Task<long> GetOrganization(string orgno)
        {
            var client = CreateClient();

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
                var result = await client.findPersonIdForIdentAsync(request);
                return result.@return.value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return 0;
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }
        }

        public async Task<long> GetPerson(string nin)
        {
            var client = CreateClient();

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
                var result = await client.findPersonIdForIdentAsync(request);
                return result.@return.value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return 0;
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }
        }

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<MatrikkelContext, Timestamp, KoordinatsystemKodeId>(_requestContextService.ServiceContext);
        }

        private PersonService CreateClient()
        {
            var endpointAddress = _settings.MatrikkelRootUrl + "PersonServiceWS";
            var serviceContext = _requestContextService.ServiceContext;

            return WcfChannelFactoryCache<PersonService>.CreateChannel(
                $"{endpointAddress}|{serviceContext}",
                new EndpointAddress(endpointAddress),
                GrunnbokHelpers.GetBasicHttpBinding(),
                credentials => GrunnbokHelpers.SetMatrikkelWSCredentials(credentials, _settings, serviceContext));
        }
    }
}
