using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces;
using Dan.Plugin.Kartverket.Config;
using Dan.Plugin.Kartverket.Models;
using Kartverket.Grunnbok.InformasjonsService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public class InformasjonsServiceClientService : IInformasjonsServiceClientService
    {
        private readonly ApplicationSettings _settings;
        private readonly ILogger _logger;
        private readonly IRequestContextService _requestContextService;

        public InformasjonsServiceClientService(IOptions<ApplicationSettings> settings, ILoggerFactory factory, IRequestContextService requestContextService)
        {
            _settings = settings.Value;
            _logger = factory.CreateLogger<InformasjonsServiceClientService>();
            _requestContextService = requestContextService;
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
            var client = CreateClient();
            HeftelseInformasjonTransfer result = null;
            try
            {
                findHeftelserRequest request = new findHeftelserRequest()
                {
                    grunnbokContext = GetContext(),
                    registerenhetId = new RegisterenhetId()
                    {
                        value = registerenhetid
                    }
                };

                var response = await client.findHeftelserAsync(request);
                result = response.@return;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }

            return result;

        }

        public async Task<HeftelseInformasjonTransfer> GetHeftelser(string registerenhetid)
        {
            var client = CreateClient();
            HeftelseInformasjonTransfer result = null;
            try
            {
                findRettigheterForRegisterenhetRequest request = new()
                {
                    grunnbokContext = GetContext(),
                    registerenhetId = new RegisterenhetId()
                    {
                        value = registerenhetid
                    }
                };

                var response = await client.findRettigheterForRegisterenhetAsync(request);
                result = response.@return;
            }catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }
            return result;

        }


        public async Task<RettsstiftelseInformasjonTransfer> GetRettsstiftelse(string rettstiftelseid)
        {
            var client = CreateClient();
            RettsstiftelseInformasjonTransfer result = null;
            try
            {
                findRettsstiftelseRequest request = new findRettsstiftelseRequest()
                {
                    grunnbokContext = GetContext(),
                    rettsstiftelseId = new RettsstiftelseId()
                    {
                        value = rettstiftelseid
                    }
                };

                var response = await client.findRettsstiftelseAsync(request);
                result = response.@return;
            }catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }

            return result;
        }

        private async Task<OverdragelseAvRegisterenhetsrettInformasjonTransfer> GetOverdragelserAvRegisterenhetsrett(string registerenhetid)
        {
            OverdragelseAvRegisterenhetsrettInformasjonTransfer result = null;
            var client = CreateClient();

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
                var response = await client.findOverdragelserAvRegisterenhetsrettAsync(request);
                result = response.@return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            finally
            {
                await ((IClientChannel)client).CloseChannelAsync();
            }

            return result;
        }

        private GrunnbokContext GetContext()
        {
            return GrunnbokHelpers.CreateGrunnbokContext<GrunnbokContext, Timestamp>(_requestContextService.ServiceContext);
        }

        private InformasjonService CreateClient()
        {
            var serviceContext = _requestContextService.ServiceContext;

            if (string.IsNullOrWhiteSpace(serviceContext))
            {
                throw new InvalidOperationException(
                    "ServiceContext is not set. Ensure SetRequestContext() is called before using InformasjonsServiceClientService.");
            }

            var endpointAddress = $"{_settings.GrunnbokRootUrl}InformasjonServiceWS";

            return WcfChannelFactoryCache<InformasjonService>.CreateChannel(
                $"{endpointAddress}|{serviceContext.ToUpperInvariant()}",
                new EndpointAddress(endpointAddress),
                GrunnbokHelpers.GetBasicHttpBinding(),
                credentials => GrunnbokHelpers.SetGrunnbokWSCredentials(credentials, _settings, serviceContext));
        }

    }
}
