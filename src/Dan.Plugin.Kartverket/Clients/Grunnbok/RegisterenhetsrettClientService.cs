using Dan.Plugin.Kartverket.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.ServiceModel;
using Kartverket.Grunnbok.RegisterenhetsrettService;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class RegisterenhetsrettClientService : IRegisterenhetsrettClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;

        private RegisterenhetsrettServiceClient _client;

        public RegisterenhetsrettClientService(ILoggerFactory factory, IOptions<ApplicationSettings> settings)
        {
            _logger = factory.CreateLogger<RegisterenhetsrettClientService>();
            _settings = settings.Value;

            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();
            _client = new RegisterenhetsrettServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokRootUrl + "RegisterenhetsrettServiceWS"));
            GrunnbokHelpers.SetGrunnbokWSCredentials(_client.ClientCredentials, _settings);
        }

        public async Task<string> GetRetterForEnheter(string registerenhetsid)
        {
            var request = new findRetterForEnheterRequest()
            {
                Body = new findRetterForEnheterRequestBody()
                {
                    grunnbokContext = GetContext(),
                    registerenhetIds = new RegisterenhetIdList()
                    {
                        new RegisterenhetId()
                        {
                            value = registerenhetsid
                        }
                    }
                }
            };

            var response = await _client.findRetterForEnheterAsync(request);

            return response.Body.@return.ToString();
        }

        private GrunnbokContext GetContext()
        {
            return new GrunnbokContext()
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

    public interface IRegisterenhetsrettClientService
    {
        public Task<string> GetRetterForEnheter(string registerenhetsid);
    }
}
