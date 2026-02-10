using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dan.Plugin.Kartverket.Clients.Grunnbok.StoreServiceClientService;

namespace Dan.Plugin.Kartverket
{
    public interface IDiHeWrapper
    {
        // Define methods for DiHeWrapper here
        Task<MotorizedTrafficResponse> GetMotorizedTrafficInformation(string identifier);

    }
    public class DiHeWrapper : IDiHeWrapper
    {
        private readonly IAddressLookupClient _geonorgeClient;
        private readonly IKartverketGrunnbokMatrikkelService _kartverketService;
        private readonly IStoreServiceClientService _storeServiceClient;

        public DiHeWrapper(IAddressLookupClient addressLookupClient, IKartverketGrunnbokMatrikkelService _kartverketGMService, IStoreServiceClientService storeServiceClient)
        {
            _geonorgeClient = addressLookupClient;
            _kartverketService = _kartverketGMService;
            _storeServiceClient = storeServiceClient;
        }

        public async Task<MotorizedTrafficResponse> GetMotorizedTrafficInformation(string identifier)
        {
            var result = new MotorizedTrafficResponse();

            var kartverketResponse = await _kartverketService.FindOwnedProperties(identifier);

            foreach (var property in kartverketResponse)
            {
                var martikkelNumber = BuildMatrikkelNumber(property.ProperyData.Kommunenummer, property.ProperyData.Gardsnummer, property.ProperyData.Bruksnummer, property.ProperyData.Festenummer, property.ProperyData.Seksjonsnummer);
                var coordinates = "";
                if(!string.IsNullOrEmpty(martikkelNumber))
                    coordinates = string.Join(", ", await _geonorgeClient.GetCoordinatesForProperty(martikkelNumber, property.ProperyData.Gardsnummer, property.ProperyData.Bruksnummer, property.ProperyData.Seksjonsnummer, property.ProperyData.Festenummer, property.ProperyData.Kommunenummer));

                result.Properties.Add( new MotorizedTrafficProperty
                {
                    MatrikkelNumber = martikkelNumber,
                    Coordinates = coordinates,
                    CoOwners = property.Owners
                });
            }

            return result;
        }

        private string BuildMatrikkelNumber(string kommuneNr, string gardsNr, string bruksNr, string festeNr, string seksjonsNr)
        {
            var stringBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(kommuneNr))
                stringBuilder.Append(kommuneNr + "-");
            if(!string.IsNullOrEmpty(gardsNr) && gardsNr != "0")
                stringBuilder.Append($"{gardsNr}");
            if(!string.IsNullOrEmpty(bruksNr) && bruksNr != "0")
                stringBuilder.Append($"/{bruksNr}");
            if(!string.IsNullOrEmpty(festeNr) && festeNr != "0")
                stringBuilder.Append($"/{festeNr}");
            if(!string.IsNullOrEmpty(seksjonsNr) && seksjonsNr != "0")
                stringBuilder.Append($"/{seksjonsNr}");

            return stringBuilder.ToString();
        }
    }
}
