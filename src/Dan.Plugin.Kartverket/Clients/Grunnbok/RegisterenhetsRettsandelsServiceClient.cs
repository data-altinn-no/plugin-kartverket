using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Grunnbok.RegisterenhetsrettsandelService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class RegisterenhetsRettsandelsServiceClientService : IRegisterenhetsRettsandelsServiceClientService
    {
        private readonly ILogger _logger;
        private readonly ApplicationSettings _settings;

        private RegisterenhetsrettsandelServiceClient _client;

        public RegisterenhetsRettsandelsServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<RegisterenhetsRettsandelsServiceClientService>();

            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            _client = new RegisterenhetsrettsandelServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokRootUrl + "RegisterenhetsrettsandelServiceWS"));
            GrunnbokHelpers.SetGrunnbokWSCredentials(_client.ClientCredentials, _settings);
        }

        public async Task<List<string>> GetAndelerForRettighetshaver(string personident)
        {
            var result = new List<string>();

            var request = new findAndelerForRettighetshavereRequest()
            {
                Body = new findAndelerForRettighetshavereRequestBody()
                {
                    personIds = new PersonIdList()
                    {
                        new()
                        {
                            value = personident
                        }
                    },
                    grunnbokContext = GetContext()
                }
            };

            try
            {
                var rettighetsresponse = await _client.findAndelerForRettighetshavereAsync(request);
                var retter = rettighetsresponse.Body.@return.Values.ToList();

                result.AddRange(retter[0].Select(x => x.value));

            }
            catch (FaultException fex)
            {
                _logger.LogError(fex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result;
        }

        private GrunnbokContext GetContext()
        {
            return new()
            {
                clientIdentification = "eDueDiligence",
                clientTraceInfo = "eDueDiligence_1",
                locale = "no_578",
                snapshotVersion = new()
                {
                    timestamp = new DateTime(9999, 1, 1, 0, 0, 0)
                },
                systemVersion = "1"
            };
        }
    }

    public interface IRegisterenhetsRettsandelsServiceClientService
    {
        public Task<List<string>> GetAndelerForRettighetshaver(string personident);
    }
}
