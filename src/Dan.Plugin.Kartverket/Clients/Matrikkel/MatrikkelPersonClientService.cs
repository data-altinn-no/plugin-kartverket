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

        private PersonServiceClient _client;

        public MatrikkelPersonClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelPersonClientService>();

            //Find ident for identifier
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            _client = new PersonServiceClient(myBinding, new EndpointAddress(_settings.MatrikkelRootUrl + "PersonServiceWS"));
            GrunnbokHelpers.SetMatrikkelWSCredentials(_client.ClientCredentials, _settings);
        }

       
        public async Task<long> GetOrganization(string orgno)
        {
            findPersonIdForIdentResponse result = null;

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
            DateTime SNAPSHOT_VERSJON_DATO = new DateTime(9999, 1, 1, 0, 0, 0);

            return new MatrikkelContext()
            {
                locale = "no_NO",
                brukOriginaleKoordinater = true,
                koordinatsystemKodeId = new KoordinatsystemKodeId()
                {
                    value = 22
                },
                klientIdentifikasjon = "eDueDiligence",
                snapshotVersion = new Timestamp()
                {
                    timestamp = SNAPSHOT_VERSJON_DATO
                },
                systemVersion = "trunk"
            };
        }        
    }

    public interface IMatrikkelPersonClientService
    {
        Task<long> GetOrganization(string orgno);

        Task<long> GetPerson(string nin);

    }
}
