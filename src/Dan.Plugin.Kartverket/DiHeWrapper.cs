using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public DiHeWrapper(IAddressLookupClient addressLookupClient, IKartverketGrunnbokMatrikkelService _kartverketGMService)
        {
            _geonorgeClient = addressLookupClient;
            _kartverketService = _kartverketGMService;
        }

        public async Task<MotorizedTrafficResponse> GetMotorizedTrafficInformation(string identifier)
        {
            // Implement the logic to get motorized traffic information
            var result = new MotorizedTrafficResponse();

            var kartverketResponse = await _kartverketService.FindOwnedProperties(identifier);
            // Use identifier to retrieve all properties
            // Properties with more owners => owner identifiers
            // For each property, retrieve coordinates from
          
            foreach (var property in kartverketResponse)
            {
                //TODO: check if festenummer and seksjonsnummer are needed
                var coordinates = await _geonorgeClient.GetCoordinatesForProperty(property.Grunnbok.gnr, property.Grunnbok.bnr, "0","0", property.Grunnbok.CountyMunicipality);
                // Process coordinates as needed - add coordinates to the result object
                //result.Properties.Add(new MotorizedTrafficProperty bla bla bla 

            }

            return result;

            
        }
    }
}
