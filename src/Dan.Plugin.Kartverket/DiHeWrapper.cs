using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Models;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
        private readonly IStoreServiceClientService _store;

        public DiHeWrapper(IAddressLookupClient addressLookupClient, IKartverketGrunnbokMatrikkelService _kartverketGMService, IStoreServiceClientService store)
        {
            _geonorgeClient = addressLookupClient;
            _kartverketService = _kartverketGMService;
            _store = store;
        }

        public async Task<MotorizedTrafficResponse> GetMotorizedTrafficInformation(string identifier)
        {
            // Implement the logic to get motorized traffic information
            var result = new MotorizedTrafficResponse();

            // Use identifier to retrieve all properties
            var kartverketResponse = await _kartverketService.FindOwnedProperties(identifier);

         
            // For each property, retrieve coordinates from
            foreach (var property in kartverketResponse)
            {
                // Properties with more owners => owner identifiers
                var coOwners = new List<CoOwner>();
                var hasCoOwners = property.Owners.Share == "1/1" ? false : true;
                if (hasCoOwners)
                {
                    //find all co-owners for the property


                }

                var martikkelNumber = BuildMatrikkelNumber(property.Grunnbok.Kommunenummer, property.Grunnbok.Gardsnummer, property.Grunnbok.Bruksnummer, property.Grunnbok.Festenummer, property.Grunnbok.Seksjonsnummer);
                var coordinates = string.Join(", ", await _geonorgeClient.GetCoordinatesForProperty(martikkelNumber, property.Grunnbok.Gardsnummer, property.Grunnbok.Bruksnummer, property.Grunnbok.Seksjonsnummer, property.Grunnbok.Festenummer, property.Grunnbok.Kommunenummer));

                result.Properties.Add( new MotorizedTrafficProperty
                {
                    MatrikkelNumber = martikkelNumber,
                    Coordinates = coordinates,
                    CoOwners = coOwners }
                );
            }

            return result;
        }

        private string BuildMatrikkelNumber(string kommuneNr, string gardsNr, string bruksNr, string festeNr, string seksjonsNr)
        {
            var stringBuilder = new StringBuilder();
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
