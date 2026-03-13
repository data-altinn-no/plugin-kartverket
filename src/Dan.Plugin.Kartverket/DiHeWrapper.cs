using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Models;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Dan.Plugin.Kartverket.Clients.ar50.Ar5repo;

namespace Dan.Plugin.Kartverket
{
    public interface IDiHeWrapper
    {
        // Define methods for DiHeWrapper here
        Task<MotorizedTrafficResponse> GetMotorizedTrafficInformation(string identifier);
        Task<LandRentalResponse> GetLandRentalInformation(string matrikkelnummer);

    }
    public class DiHeWrapper : IDiHeWrapper
    {
        private readonly IAddressLookupClient _geonorgeClient;
        private readonly IKartverketGrunnbokMatrikkelService _kartverketService;
        private readonly IAr5Repo _ar50Repo;
        public DiHeWrapper(IAddressLookupClient addressLookupClient, IKartverketGrunnbokMatrikkelService _kartverketGMService, IAr5Repo ar50Repo)
        {
            _geonorgeClient = addressLookupClient;
            _kartverketService = _kartverketGMService;
            _ar50Repo = ar50Repo;
        }

        public async Task<LandRentalResponse> GetLandRentalInformation(string matrikkelNumber)
        {
            var jordTypeList = new List<JordType>();

            var coordinates = await _geonorgeClient.GetCoordinatesForProperty(matrikkelNumber);
            if (coordinates.Count > 0)
            {
                foreach (var coordinateset in coordinates)
                {
                    var ar5Response = await _ar50Repo.GetOmrade(coordinateset);
                    if (ar5Response is null)
                        continue;

                    foreach (var jordtype in ar5Response)
                    {
                        jordTypeList.Add(new JordType
                        {
                            FeatureId = jordtype.Objectid,
                            ArealType = jordtype.ArealType.ToString(),
                            Areal = jordtype.ShapeArea,
                            GeoJson = jordtype.Shape
                        });
                    }
                    
                }                
            }          

            return new LandRentalResponse
            {
                Matrikkelnumber = matrikkelNumber,
                JordType = jordTypeList
            };
        }

        public async Task<MotorizedTrafficResponse> GetMotorizedTrafficInformation(string identifier)
        {
            var result = new MotorizedTrafficResponse();

            var kartverketResponse = await _kartverketService.FindOwnedProperties(identifier);
            foreach (var property in kartverketResponse)
            {
                var martikkelNumber = BuildMatrikkelNumber(property.PropertyData.Kommunenummer, property.PropertyData.Gardsnummer, property.PropertyData.Bruksnummer, property.PropertyData.Festenummer);
                var coordinates = "";
                if(!string.IsNullOrEmpty(martikkelNumber))
                    coordinates = string.Join(", ", await _geonorgeClient.GetCoordinatesForProperty(martikkelNumber, property.PropertyData.Gardsnummer, property.PropertyData.Bruksnummer, property.PropertyData.Seksjonsnummer, property.PropertyData.Festenummer, property.PropertyData.Kommunenummer));

                result.Properties.Add( new MotorizedTrafficProperty
                {
                    MatrikkelNumber = martikkelNumber,
                    Coordinates = coordinates,
                    CoOwners = property.Owners
                });
            }

            return result;
        }

        private string BuildMatrikkelNumber(string kommuneNr, string gardsNr, string bruksNr, string festeNr)
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

            return stringBuilder.ToString();
        }
    }
}
