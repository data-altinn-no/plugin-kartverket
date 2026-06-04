using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces;
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
        private readonly ApplicationSettings _settings;
        private readonly ILogger _logger;
        private readonly IRequestContextService _requestContextService;

        public OverfoeringServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<OverfoeringServiceClientService>();
            _requestContextService = requestContextService;
        }

        public async Task<string> GetOverfoeringerTil(List<string> ids)
        {
            findOverfoeringerForOverfoerteTilResponse result = null;
            var client = CreateClient();
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
                result = await client.findOverfoeringerForOverfoerteTilAsync(request);
                return result.Body?.ToString();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error calling OverfoeringServiceClientService.GetOverfoeringerTil");
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }

            return string.Empty;
        }

        private GrunnbokContext GetContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext,Timestamp>(_requestContextService.ServiceContext);
        }

        private OverfoeringService CreateClient()
        {
            var serviceContext = _requestContextService.ServiceContext;

            if (string.IsNullOrWhiteSpace(serviceContext))
            {
                throw new InvalidOperationException(
                    "ServiceContext is not set. Ensure SetRequestContext() is called before using OverfoeringServiceClientService.");
            }

            var endpointAddress = $"{_settings.GrunnbokRootUrl}OverfoeringServiceWS";

            return WcfChannelFactoryCache<OverfoeringService>.CreateChannel(
                $"{endpointAddress}|{serviceContext}",
                new EndpointAddress(endpointAddress),
                GrunnbokHelpers.GetBasicHttpBinding(),
                credentials => GrunnbokHelpers.SetGrunnbokWSCredentials(credentials, _settings, serviceContext));
        }

    }
}
