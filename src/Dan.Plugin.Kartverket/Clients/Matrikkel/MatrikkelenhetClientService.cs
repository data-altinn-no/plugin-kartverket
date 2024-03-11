using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Matrikkel.MatrikkelenhetService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Dan.Plugin.Kartverket.Models;
using Teig = Kartverket.Matrikkel.MatrikkelenhetService.Teig;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel
{
    public class MatrikkelenhetClientService : IMatrikkelenhetClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;

        private MatrikkelenhetServiceClient _client;

        public MatrikkelenhetClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelenhetServiceClient>();

            //Find ident for identifier
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            _client = new MatrikkelenhetServiceClient(myBinding, new EndpointAddress(_settings.MatrikkelRootUrl + "MatrikkelenhetServiceWS"));
            GrunnbokHelpers.SetMatrikkelWSCredentials(_client.ClientCredentials, _settings);
        }

        public async Task<List<MatrikkelenhetId>> GetMatrikkelenheterForPerson(long ident)
        {
            findEideMatrikkelenheterForPersonResponse result = null;
            var request = new findEideMatrikkelenheterForPersonRequest()
            {
                matrikkelContext = GetContext(),
                personId = new PersonId()
                {
                    value = ident
                }
            };

            try
            {
                result = await _client.findEideMatrikkelenheterForPersonAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return result.@return.ToList();

        }

        public async Task<MatrikkelenhetMedTeigerTransfer> GetMatrikkelEnhetMedTeiger(int gnr, int bnr, int fnr, int seksjonsnummer, string kommuneIdent)
        {
            findMatrikkelenhetMedTeigerResponse result = null;

            var request = new findMatrikkelenhetMedTeigerRequest()
            {
                matrikkelContext = GetContext(),
                matrikkelenhetIdent = new MatrikkelenhetIdent()
                {
                    bruksnummer = bnr,
                    bruksnummerSpecified = true,
                    gardsnummer = gnr,
                    gardsnummerSpecified = true,
                    festenummer = fnr,
                    festenummerSpecified = true,
                    seksjonsnummer = seksjonsnummer,
                    kommuneIdent = new KommuneIdent()
                    {
                        kommunenummer = kommuneIdent
                    },
                    seksjonsnummerSpecified = true
                },

            };

            try
            {
                result = await _client.findMatrikkelenhetMedTeigerAsync(request);
                return result.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return new findMatrikkelenhetMedTeigerResponse().@return;
        }

        public async Task<MatrikkelEnhetMedteig> GetMatrikkelEnhetTeig(int gnr, int bnr, int fnr, int seksjonsnummer, string kommuneIdent)
        {
            var response = await GetMatrikkelEnhetMedTeiger(gnr, bnr, fnr, seksjonsnummer, kommuneIdent);

            var matrikkelEnhet = response.bubbleObjects.OfType<Grunneiendom>().FirstOrDefault();
            var teiger = response.bubbleObjects.OfType<Teig>().Where(y=>y.tvist == false).Select(x => x.lagretBeregnetAreal).ToList();

            var result = new MatrikkelEnhetMedteig()
            {
                Bruksnummer = bnr.ToString(),
                Gaardsnummer = gnr.ToString(),
                HasCulturalHeritageSite = matrikkelEnhet != null ? matrikkelEnhet.harKulturminne : false,
                Teiger = teiger
            };

            return result;
        }

        public async Task<MatrikkelenhetId> GetMatrikkelenhet(int gnr, int bnr, int fnr, int seksjonsnummer, string kommuneIdent)
        {
            findMatrikkelenhetResponse result = null;

            var request = new findMatrikkelenhetRequest()
            {
                matrikkelContext = GetContext(),
                matrikkelenhetIdent = new MatrikkelenhetIdent()
                {
                    bruksnummer = bnr,
                    bruksnummerSpecified = true,
                    gardsnummer = gnr,
                    gardsnummerSpecified = true,
                    festenummer = fnr,
                    festenummerSpecified = true,
                    seksjonsnummer = seksjonsnummer,
                    kommuneIdent = new KommuneIdent()
                    {
                        kommunenummer = kommuneIdent
                    },
                    seksjonsnummerSpecified = true
                },
                
            };

            try
            {
                result = await _client.findMatrikkelenhetAsync(request);
                return result.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return new MatrikkelenhetId();
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

    public interface IMatrikkelenhetClientService
    {
        Task<MatrikkelenhetId> GetMatrikkelenhet(int gnr, int bnr, int fnr, int seksjonsnummer, string kommuneIdent);
        Task<List<MatrikkelenhetId>> GetMatrikkelenheterForPerson(long ident);
        Task<MatrikkelenhetMedTeigerTransfer> GetMatrikkelEnhetMedTeiger(int gnr, int bnr, int fnr, int seksjonsnummer, string kommuneIdent);

        Task<MatrikkelEnhetMedteig> GetMatrikkelEnhetTeig(int gnr, int bnr, int fnr, int seksjonsnummer, string kommuneIdent);
    }
}
