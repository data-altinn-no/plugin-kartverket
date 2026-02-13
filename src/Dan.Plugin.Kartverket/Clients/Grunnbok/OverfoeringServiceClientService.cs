using Dan.Plugin.Kartverket.Config;
using Kartverket.Grunnbok.OverfoeringService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class OverfoeringServiceClientService : IOverfoeringServiceClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;
        private OverfoeringServiceClient _client;
        private IRequestContextService _requestContextService;

        public OverfoeringServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<IdentServiceClientService>();
            _requestContextService = requestContextService;

            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();
            _client = new OverfoeringServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokRootUrl + "OverfoeringServiceWS"));
            GrunnbokHelpers.SetGrunnbokWSCredentials(_client.ClientCredentials, _settings);
        }

        public async Task<string> GetOverfoeringerTil(List<string> ids)
        {
            findOverfoeringerForOverfoerteTilResponse result = null;

            var inputList = new RegisterenhetsrettIdList();

            foreach (var id in ids)
            {
                inputList.Add(new RegisterenhetsrettId()
                {
                    value = id
                });
            }

            findOverfoeringerForOverfoerteTilRequest request = new()
            {
                Body = new findOverfoeringerForOverfoerteTilRequestBody()
                {
                    grunnbokContext = GetContext(),
                    registerenhetsrettIds = inputList,
                    overfoerteIds = new RettsstiftelseIdList(),
                    registerenhetsrettsandelIds = new RegisterenhetsrettsandelIdList()
                }
            };

            try
            {
                result = await _client.findOverfoeringerForOverfoerteTilAsync(request);
               
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return string.Empty;
        }

        private GrunnbokContext GetContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext,Timestamp>(_requestContextService.ServiceContext);
        }
    }

    public interface IOverfoeringServiceClientService
    {
        public Task<string> GetOverfoeringerTil(List<string> ids);
    }
}
