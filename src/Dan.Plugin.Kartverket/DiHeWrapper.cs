using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.ar50;
using Dan.Plugin.Kartverket.Clients.Grunnbok;
using Dan.Plugin.Kartverket.Models;
using Microsoft.Extensions.FileProviders;
using NetTopologySuite.Geometries;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static Dan.Plugin.Kartverket.Clients.ar50.ar50repo;

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
        private readonly Iar50Repo _ar50Repo;
        public DiHeWrapper(IAddressLookupClient addressLookupClient, IKartverketGrunnbokMatrikkelService _kartverketGMService, Iar50Repo ar50Repo)
        {
            _geonorgeClient = addressLookupClient;
            _kartverketService = _kartverketGMService;
            _ar50Repo = ar50Repo;
        }

        public async Task<LandRentalResponse> GetLandRentalInformation(string matrikkelNumber)
        {
            var result = new LandRentalResponse();
            
            var coordinates = await _geonorgeClient.GetCoordinatesForProperty(matrikkelNumber);
            if (coordinates != null)
            {
                foreach (var coordinateset in coordinates)
                {
                    var ar5Response = await _ar50Repo.GetOmrade(coordinateset);
                    result = new LandRentalResponse
                    {
                        Matrikkelnumber = matrikkelNumber,
                        JordType = new JordType
                        {
                            FeatureId = ar5Response.Objectid,
                            ArealType = ar5Mapper.MapArealType(ar5Response.ArealType),
                            Areal = ar5Response.ShapeArea,
                            GeoJson = ar5Response.Shape
                        }
                    };
                }                
            }          

            return result;
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
