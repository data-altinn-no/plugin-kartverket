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
        private IRequestContextService _requestContextService;

        public MatrikkelenhetClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<MatrikkelenhetServiceClient>();
            _requestContextService = requestContextService;
        }

        public async Task<List<MatrikkelenhetId>> GetMatrikkelenheterForPerson(long ident)
        {
            findEideMatrikkelenheterForPersonResponse result = null;
            var client = CreateClient();

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
                result = await client.findEideMatrikkelenheterForPersonAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                try { await client.CloseAsync(); }
                catch { client.Abort(); }
            }

            return result.@return.ToList();

        }

        public async Task<MatrikkelenhetMedTeigerTransfer> GetMatrikkelEnhetMedTeiger(int gnr, int bnr, int fnr, int seksjonsnummer, string kommuneIdent)
        {
            findMatrikkelenhetMedTeigerResponse result = null;
            var client = CreateClient();

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
                result = await client.findMatrikkelenhetMedTeigerAsync(request);
                return result.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                try { await client.CloseAsync(); }
                catch { client.Abort(); }
            }

            return new findMatrikkelenhetMedTeigerResponse().@return;
        }

        public async Task<MatrikkelEnhetMedteig> GetMatrikkelEnhetTeig(int gnr, int bnr, int fnr, int seksjonsnummer, string kommuneIdent)
        {
            var response = await GetMatrikkelEnhetMedTeiger(gnr, bnr, fnr, seksjonsnummer, kommuneIdent);

            if (response == null)
            {
                return new MatrikkelEnhetMedteig();
            }
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
            var client = CreateClient();

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
                result = await client.findMatrikkelenhetAsync(request);
                return result.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                try { await client.CloseAsync(); }
                catch { client.Abort(); }
            }

            return new MatrikkelenhetId();
        }

        private MatrikkelContext GetContext()
        {
            return GrunnbokHelpers.CreateMatrikkelContext<MatrikkelContext, Timestamp, KoordinatsystemKodeId>(_requestContextService.ServiceContext);
        }        

        private MatrikkelenhetServiceClient CreateClient()
        {
            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();

            var client = new MatrikkelenhetServiceClient(
                myBinding,
                new EndpointAddress(_settings.MatrikkelRootUrl + "MatrikkelenhetServiceWS")
            );

            GrunnbokHelpers.SetMatrikkelWSCredentials(
                client.ClientCredentials,
                _settings,
                _requestContextService.ServiceContext
            );

            return client;
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
