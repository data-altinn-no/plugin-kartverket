using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.ar50;
using Dan.Plugin.Kartverket.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            var propertyHasFritidsbolig = await _kartverketService.PropertyHasFritidsbolig(matrikkelNumber);

            if (coordinates.Count > 0)
            {
                foreach (var coordinateSet in coordinates)
                {
                    var ar5Response = await _ar50Repo.GetOmrade(coordinateSet);

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

            var addressResponse = await _kartverketService.GetAdresseByMatrikkelNumber(matrikkelNumber);           

            return new LandRentalResponse
            {
                Matrikkelnumber = matrikkelNumber,
                JordType = jordTypeList,
                Adresse = addressResponse,
                EiendomHarFritidsbolig = propertyHasFritidsbolig
            };
        }

        public async Task<MotorizedTrafficResponse> GetMotorizedTrafficInformation(
           string identifier
       )
        {
            var result = new MotorizedTrafficResponse();

            var kartverketResponse = await _kartverketService.FindOwnedProperties(identifier);
            foreach (var property in kartverketResponse)
            {
                var martikkelNumber = BuildMatrikkelNumber(
                    property.PropertyData.Kommunenummer,
                    property.PropertyData.Gardsnummer,
                    property.PropertyData.Bruksnummer,
                    property.PropertyData.Festenummer
                );

                var coordinates = new List<List<double>>();
                if (!string.IsNullOrEmpty(martikkelNumber))
                    coordinates = await _geonorgeClient.GetCoordinatesForProperty(
                        martikkelNumber,
                        property.PropertyData.Gardsnummer,
                        property.PropertyData.Bruksnummer,
                        property.PropertyData.Seksjonsnummer,
                        property.PropertyData.Festenummer,
                        property.PropertyData.Kommunenummer
                    );

                var adresser = new List<Address>();
                //sometimes we get the matrikkelnummer instead of streetname, so we check if the
                //it is a matrikkelnumber first, and split it up if it is
                var matrikkelpattern = @"^\d+/\d+/\d+$";
                var matrikkelpattern2 = @"^\d{4}-\d+/\d+/\d+$";
                if (property.Addresses.Any())
                {

                    foreach (var address in property.Addresses)
                    {
                        string kommunenr = null;
                        string gnr = null;
                        string bnr = null;
                        string fnr = null;

                        if (Regex.IsMatch(address.Street, matrikkelpattern))
                        {
                            var parts = address.Street.Split('/');
                            gnr = parts[0];
                            bnr = parts[1];
                            fnr = parts[2];
                        }
                        else if (Regex.IsMatch(address.Street, matrikkelpattern2))
                        {
                            var parts = address.Street.Split('-', '/');
                            kommunenr = parts[0];
                            gnr = parts[1];
                            bnr = parts[2];
                            fnr = parts[3];
                        }

                        var data = await _geonorgeClient.SearchByMatrikkelNumber(
                            kommunenr ?? property.PropertyData.Kommunenummer,
                            gnr ?? property.PropertyData.Gardsnummer,
                            bnr ?? property.PropertyData.Bruksnummer,
                            fnr ?? property.PropertyData.Festenummer,
                            address.Street,
                            address.PostalCode,
                            address.City);

                        foreach (var adresse in data.Adresser)
                        {
                            adresser.Add(new Address
                            {
                                Street = adresse.Adressetekst,
                                PostalCode = adresse.Postnummer,
                                City = adresse.Poststed,
                            });
                        }
                    }
                    //sometimes duplicates can happen after looking up the address 
                    //filter out duplicates
                    adresser = adresser
                        .Where(a => !string.IsNullOrWhiteSpace(a.Street) &&
                                !string.IsNullOrWhiteSpace(a.PostalCode) &&
                                !string.IsNullOrWhiteSpace(a.City))
                    .GroupBy(a => new
                    {
                        Street = a.Street?.Trim().ToLower(),
                        PostalCode = a.PostalCode?.Trim(),
                        City = a.City?.Trim().ToLower()
                    })
                    .Select(g => g.First())
                    .ToList();
                                       
                }
                
                result.Properties.Add(
                    new MotorizedTrafficProperty
                    {
                        MatrikkelNumber = martikkelNumber,
                        Coordinates = coordinates,
                        CoOwners = property.Owners,
                        Address = adresser,
                        IsFritidsbolig = property.IsFritidsbolig,
                    }
                );
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
