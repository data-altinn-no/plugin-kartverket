using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Kartverket.Matrikkel.BygningService;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel
{
    public class MatrikkelBygningClientService : IMatrikkelBygningClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;
        private IRequestContextService _requestContextService;
        public MatrikkelBygningClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelBygningClientService>();
            _requestContextService = requestContextService; 
        }

        public async Task<List<long>> GetBygningerForMatrikkelenhet(long matrikkelEnhetId)
        {
            List<long> result = new List<long>();
            var client = CreateClient();

            var request = new findByggForMatrikkelenhetRequest()
            {
                matrikkelContext = GetContext(),
                matrikkelenhetId = new MatrikkelenhetId()
                {
                    value = matrikkelEnhetId
                }
            };

            try
            {
                var response = await client.findByggForMatrikkelenhetAsync(request);
                result.AddRange(response.@return.Select(x=>x.value).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                try { client.Close(); }
                catch{ client.Abort();}
            }

            return result;
        }

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<MatrikkelContext, Timestamp, KoordinatsystemKodeId>(_requestContextService.ServiceContext);
        }

        private BygningServiceClient CreateClient()
        {
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            var client = new BygningServiceClient(
                myBinding,
                new EndpointAddress(_settings.MatrikkelRootUrl + "BygningServiceWS")
            );

            GrunnbokHelpers.SetMatrikkelWSCredentials(
                client.ClientCredentials,
                _settings,
                _requestContextService.ServiceContext
            );

            return client;
        }

        private BygningServiceClient CreateClient()
        {
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            var client = new BygningServiceClient(
                myBinding,
                new EndpointAddress(_settings.MatrikkelRootUrl + "BygningServiceWS")
            );

            GrunnbokHelpers.SetMatrikkelWSCredentials(
                client.ClientCredentials,
                _settings,
                _requestContextService.ServiceContext
            );

            return client;
        }
    }

    public interface IMatrikkelBygningClientService
    {
        Task<List<long>> GetBygningerForMatrikkelenhet(long matrikkelEnhetId);
    }
}
