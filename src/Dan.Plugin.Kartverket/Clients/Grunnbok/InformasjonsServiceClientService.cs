using Dan.Plugin.Kartverket.Config;
using Kartverket.Grunnbok.InformasjonsService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Dan.Plugin.Kartverket.Models;
using GrunnbokContext = Kartverket.Grunnbok.InformasjonsService.GrunnbokContext;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class InformasjonsServiceClientService : IInformasjonsServiceClientService
    {
        private ApplicationSettings _settings;
        private ILogger _logger;

        private InformasjonServiceClient _client;

        public InformasjonsServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<InformasjonsServiceClientService>();

            var myBinding = GrunnbokHelpers.GetBasicHttpBinding();
            myBinding.MaxReceivedMessageSize = int.MaxValue;
            string identity = string.Empty;

            _client = new InformasjonServiceClient(myBinding, new EndpointAddress(_settings.GrunnbokBaseUrl + "InformasjonServiceWS"));
            GrunnbokHelpers.SetCredentials(_client.ClientCredentials, _settings, ServiceContext.Grunnbok);
        }

        private GrunnbokContext GetContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext,Timestamp>();
        }

        public async Task<OwnerShipTransferInfo> GetOwnershipInfo(string registerenhetid)
        {
            OwnerShipTransferInfo result = null;

            var grunnbokData = await GetOverdragelserAvRegisterenhetsrett(registerenhetid);

            if (grunnbokData != null)
            {
                var overdragelse = grunnbokData.bubbleObjects.OfType<OverdragelseAvRegisterenhetsrett>().FirstOrDefault();
                var currency = grunnbokData.bubbleObjects.OfType<ValutakodeKode>().FirstOrDefault();

                if (overdragelse != null)
                {
                    result = new OwnerShipTransferInfo()
                    {
                        Price = overdragelse?.omsetning?.vederlag?.beloepsverdi,
                        CurrencyCode = currency.kodeverdi,
                        EstablishedDate = overdragelse.oppdateringsdato.timestamp
                    };
                }
            }

            return result;
        }

        public async Task<HeftelseInformasjonTransfer> GetPawnStuff(string registerenhetid)
        {
            findHeftelserRequest request = new findHeftelserRequest()
            {
                grunnbokContext = GetContext(),
                registerenhetId = new RegisterenhetId()
                {
                    value = registerenhetid
                }
            };

            var result = await _client.findHeftelserAsync(request);

            return result.@return;
        }

        public async Task<HeftelseInformasjonTransfer> GetHeftelser(string registerenhetid)
        {
            findRettigheterForRegisterenhetRequest request = new()
            {
                grunnbokContext = GetContext(),
                registerenhetId = new RegisterenhetId()
                {
                    value = registerenhetid
                }
            };

            var result = await _client.findRettigheterForRegisterenhetAsync(request);

            return result.@return;
        }


        public async Task<RettsstiftelseInformasjonTransfer> GetRettsstiftelse(string rettstiftelseid)
        {
            findRettsstiftelseRequest request = new findRettsstiftelseRequest()
            {
                grunnbokContext = GetContext(),
                rettsstiftelseId = new RettsstiftelseId()
                {
                    value = rettstiftelseid
                }
            };

            var result = await _client.findRettsstiftelseAsync(request);

            return result.@return;
        }
        private async Task<OverdragelseAvRegisterenhetsrettInformasjonTransfer> GetOverdragelserAvRegisterenhetsrett(string registerenhetid)
        {
            OverdragelseAvRegisterenhetsrettInformasjonTransfer result = null;

            var request = new findOverdragelserAvRegisterenhetsrettRequest()
            {
                grunnbokContext = GetContext(),
                registerenhetId = new RegisterenhetId()
                {
                    value = registerenhetid
                }
            };

            try
            {
                var response = await _client.findOverdragelserAvRegisterenhetsrettAsync(request);
                result = response.@return;

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
    }

    public interface IInformasjonsServiceClientService
    {
        public Task<OwnerShipTransferInfo> GetOwnershipInfo(string registerenhetsid);

        public Task<HeftelseInformasjonTransfer> GetPawnStuff(string registerenhetid);

        public Task<HeftelseInformasjonTransfer> GetHeftelser(string registerenhetid);

        public Task<RettsstiftelseInformasjonTransfer> GetRettsstiftelse(string rettstiftelseid);
    }
}
