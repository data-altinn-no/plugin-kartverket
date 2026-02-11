using System;
using System.Collections.Generic;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Grunnbok.OverfoeringService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class OverfoeringServiceClientService : IOverfoeringServiceClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;
        private OverfoeringServiceClient _client;

        public OverfoeringServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<IdentServiceClientService>();

            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();
            _client = new OverfoeringServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokRootUrl + "OverfoeringServiceWS"));
            GrunnbokHelpers.SetCredentials(_client.ClientCredentials, _settings, ServiceContext.Grunnbok);
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
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext,Timestamp>();
        }
    }

    public interface IOverfoeringServiceClientService
    {
        public Task<string> GetOverfoeringerTil(List<string> ids);
    }
}
