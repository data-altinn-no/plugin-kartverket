using System;
using System.Collections.Generic;
using System.Linq;
using Dan.Plugin.Kartverket.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ServiceModel;
using System.Threading.Tasks;
using Dan.Plugin.Kartverket.Models;
using Kartverket.Grunnbok.RettsstiftelseService;
using GrunnbokContext = Kartverket.Grunnbok.RettsstiftelseService.GrunnbokContext;
using RegisterenhetsrettId = Kartverket.Grunnbok.RettsstiftelseService.RegisterenhetsrettId;
using TransferMode = Kartverket.Grunnbok.RettsstiftelseService.TransferMode;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class RettsstiftelseClientService : IRettsstiftelseClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;
        private RettsstiftelseServiceClient _client;

        public RettsstiftelseClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<RettsstiftelseClientService>();

            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();
            _client = new RettsstiftelseServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokRootUrl + "RettsstiftelseServiceWS"));
            GrunnbokHelpers.SetGrunnbokWSCredentials(_client.ClientCredentials, _settings);
        }

        public async Task<findOverdragelserAvRegisterenhetsrettForPersonResponse> GetOverdragelserAvRegisterenhetsrett(string ident)
        {
            findOverdragelserAvRegisterenhetsrettForPersonResponse result = null;

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

        public async Task<findAktiveRettsstiftelserMedAktivRettighetshaverEllerSaksoekerResponse> GetAktiveRettsstiftelser(List<string> idents)
        {
            findAktiveRettsstiftelserMedAktivRettighetshaverEllerSaksoekerResponse result = null;

            PersonId[] ids = new PersonId[idents.Count - 1];

            int i = 0;
            foreach (var id in idents)
            {
                ids[i].value = id;
            }

            findAktiveRettsstiftelserMedAktivRettighetshaverEllerSaksoekerRequest request = new()
            {
                grunnbokContext = GetContext(),
                personIds = ids,
            };
            
            try
            {
                result = await _client.findAktiveRettsstiftelserMedAktivRettighetshaverEllerSaksoekerAsync(request);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return result;
        }

        public async Task<findRettigheterForRegisterenhetResponse> GetRettighetForRegisterenhet(string registerenhetid, string kommuneid)
        {
            findRettigheterForRegisterenhetResponse result = null;

            findRettigheterForRegisterenhetRequest request = new()
            {
                grunnbokContext = GetContext(),
                kommuneIds = new KommuneId[] { new KommuneId() {value = kommuneid } },
                registerenhetId = new RegisterenhetId() { value = registerenhetid }
            };

            try
            {
                result = await _client.findRettigheterForRegisterenhetAsync(request);

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

    public interface IRettsstiftelseClientService
    {
        public Task<findAktiveRettsstiftelserMedAktivRettighetshaverEllerSaksoekerResponse> GetAktiveRettsstiftelser(List<string> ids);

        public Task<findRettigheterForRegisterenhetResponse> GetRettighetForRegisterenhet(string registerenhetid, string kommuneid);

        public Task<List<PawnDocument>> GetHeftelser(string registerenhetid);

        //public Task<List<RettsstiftelseId>> GetRettigheterForPerson(string personId);

        //public Task<List<RettsstiftelseId>> GetRettigheterForRegisterenhet(string registerenhetId, string kommuneId);
    }
}
