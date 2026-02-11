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

        private BygningServiceClient _client;

        public MatrikkelBygningClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelBygningClientService>();

            //Find ident for identifier
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            _client = new BygningServiceClient(myBinding, new EndpointAddress(_settings.MatrikkelRootUrl + "BygningServiceWS"));
            GrunnbokHelpers.SetCredentials(_client.ClientCredentials, _settings, ServiceContext.Matrikkel);
        }

        public async Task<List<long>> GetBygningerForMatrikkelenhet(long matrikkelEnhetId)
        {
            List<long> result = new List<long>();

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
                var response = await _client.findByggForMatrikkelenhetAsync(request);
                result.AddRange(response.@return.Select(x=>x.value).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result;
        }

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<MatrikkelContext, Timestamp>();
        }
    }

    public interface IMatrikkelBygningClientService
    {
        Task<List<long>> GetBygningerForMatrikkelenhet(long matrikkelEnhetId);
    }
}
