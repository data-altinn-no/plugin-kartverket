using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using Kartverket.Grunnbok.RettsstiftelseService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using GrunnbokContext = Kartverket.Grunnbok.RettsstiftelseService.GrunnbokContext;
using TransferMode = Kartverket.Grunnbok.RettsstiftelseService.TransferMode;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class RettsstiftelseClientService : IRettsstiftelseClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;
        private RettsstiftelseServiceClient _client;
        private IRequestContextService _requestContextService;

        public RettsstiftelseClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<RettsstiftelseClientService>();
            _requestContextService = requestContextService;            
        }

        public async Task<findOverdragelserAvRegisterenhetsrettForPersonResponse> GetOverdragelserAvRegisterenhetsrett(string ident)
        {
            findOverdragelserAvRegisterenhetsrettForPersonResponse result = null;
            var _client = CreateClient();

            findOverdragelserAvRegisterenhetsrettForPersonRequest request = new()
            {
                grunnbokContext = GetContext(),
                personId = new PersonId()
                {
                    value = ident
                },
                kommuneIds = null
            };

            try
            {
                result = await _client.findOverdragelserAvRegisterenhetsrettForPersonAsync(request);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return result;
        }

        public async Task<List<PawnDocument>> GetHeftelser(string registerenhetid)
        {
            List<PawnDocument> result = new List<PawnDocument>();
            var _client = CreateClient();

            findHeftelserRequest request = new()
            {
                grunnbokContext = GetContext(),
                registerenhetId = new RegisterenhetId()
                {
                    value = registerenhetid
                },
                transferMode = TransferMode.Objects
            };

            var response = await _client.findHeftelserAsync(request);
            var responseObject = response.@return.bubbleObjects.OfType<Pant>();
            
            foreach (var pawn in responseObject)
            {
                var amounts = new List<Amount>();

                for (int i = 0; i < pawn.beloep.Length; i++)
                {
                    amounts.Add(new Amount()
                    {
                        CurrencyCode = pawn.beloep[i].valutakodeId.value == "5" ? "NOK" : "",
                        AmountText = pawn.beloep[i].beloepstekst,
                        Sum = pawn.beloep[i].beloepsverdi
                    });
                }

                var temp = new PawnDocument()
                {
                    Amounts = amounts,
                    OwnerId = long.Parse(pawn.rettighetshavereIds[0].value),
                    Owner = ""
                };

                result.Add(temp);
            }
            

            return result;
        }

        private GrunnbokContext GetContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext,Timestamp>(_requestContextService.ServiceContext);
        }

        private RettsstiftelseServiceClient CreateClient()
        {
            var serviceContext = _requestContextService.ServiceContext;

            if (string.IsNullOrWhiteSpace(serviceContext))
                throw new InvalidOperationException(
                    "ServiceContext is not set. Ensure SetRequestContext() is called before using RettsstiftelseClientService.");

            var binding = GrunnbokHelpers.GetBasicHttpBinding();
            var endpoint = new EndpointAddress(
                $"{_settings.GrunnbokRootUrl}RettsstiftelseServiceWS");

            var client = new RettsstiftelseServiceClient(binding, endpoint);

            GrunnbokHelpers.SetGrunnbokWSCredentials(
                client.ClientCredentials,
                _settings,
                serviceContext);

            return client;
        }

    }

    public interface IRettsstiftelseClientService
    {
        public Task<List<PawnDocument>> GetHeftelser(string registerenhetid);

    }
}
