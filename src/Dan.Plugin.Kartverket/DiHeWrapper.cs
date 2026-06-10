using Dan.Plugin.Kartverket.Clients;
using Dan.Plugin.Kartverket.Clients.ar50;
using Dan.Plugin.Kartverket.Models;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly ILogger<DiHeWrapper> _logger;

        public DiHeWrapper(IAddressLookupClient addressLookupClient, IKartverketGrunnbokMatrikkelService _kartverketGMService, IAr5Repo ar50Repo, ILogger<DiHeWrapper> logger)
        {
            _geonorgeClient = addressLookupClient;
            _kartverketService = _kartverketGMService;
            _ar50Repo = ar50Repo;
            _logger = logger;
        }

        public async Task<LandRentalResponse> GetLandRentalInformation(string matrikkelNumber)
        {
            // Run coordinates, fritidsbolig check and address lookup in parallel
            var coordinatesTask = _geonorgeClient.GetCoordinatesForLandRental(matrikkelNumber, includeWholeOmrade: true);
            var fritidsboligTask = _kartverketService.PropertyHasFritidsbolig(matrikkelNumber);
            var addressResponseTask = _kartverketService.GetAdresseByMatrikkelNumber(matrikkelNumber);

            await Task.WhenAll(coordinatesTask, fritidsboligTask, addressResponseTask);

            var coordinates = await coordinatesTask;
            var propertyHasFritidsbolig = await fritidsboligTask;
            var addressResponse = await addressResponseTask;

            // Run all AR5 area lookups in parallel
            var jordTypeList = new List<JordType>();
            if (coordinates.Count > 0)
            {
                var ar5Results = await Task.WhenAll(coordinates.Select(c => _ar50Repo.GetOmrade(c)));

                foreach (var ar5Response in ar5Results)
                {
                    if (ar5Response is null) continue;
                    jordTypeList.AddRange(ar5Response.Select(jordtype => new JordType
                    {
                        FeatureId = jordtype.Objectid,
                        ArealType = jordtype.ArealType.ToString(),
                        Areal = jordtype.ClippedArea,
                        GeoJson = jordtype.GeoJson,
                    }));
                }
            }

            // Parse fallback values from matrikkelNumber and run address lookups in parallel
            var parts = matrikkelNumber.Split('-', '/');
            var adresse = await GetAdresserForProperty(
                addressResponse,
                fallbackKommunenr: parts.ElementAtOrDefault(0),
                fallbackGnr: parts.ElementAtOrDefault(1),
                fallbackBnr: parts.ElementAtOrDefault(2),
                fallbackFnr: parts.ElementAtOrDefault(3));

            return new LandRentalResponse
            {
                Matrikkelnumber = matrikkelNumber,
                JordType = jordTypeList,
                Adresse = adresse,
                EiendomHarFritidsbolig = propertyHasFritidsbolig
            };
        }

        public async Task<MotorizedTrafficResponse> GetMotorizedTrafficInformation(string identifier)
        {
            var result = new MotorizedTrafficResponse();
            var kartverketResponse = await _kartverketService.FindOwnedProperties(identifier);

            var propertyTasks = kartverketResponse.Select(async property =>
            {
                try
                {
                    if (property?.PropertyData == null)
                        return null;

                    var martikkelNumber = BuildMatrikkelNumber(
                        property.PropertyData.Kommunenummer,
                        property.PropertyData.Gardsnummer,
                        property.PropertyData.Bruksnummer,
                        property.PropertyData.Festenummer,
                        property.PropertyData.Seksjonsnummer
                    );

                    var coordinatesTask = string.IsNullOrEmpty(martikkelNumber)
                        ? Task.FromResult(new List<List<List<double>>>())
                        : _geonorgeClient.GetCoordinatesForProperty(
                            martikkelNumber,
                            property.PropertyData.Gardsnummer,
                            property.PropertyData.Bruksnummer,
                            property.PropertyData.Seksjonsnummer,
                            property.PropertyData.Festenummer,
                            property.PropertyData.Kommunenummer);

                    var adresseTask = GetAdresserForProperty(
                        property.Addresses,
                        fallbackKommunenr: property.PropertyData.Kommunenummer,
                        fallbackGnr: property.PropertyData.Gardsnummer,
                        fallbackBnr: property.PropertyData.Bruksnummer,
                        fallbackFnr: property.PropertyData.Festenummer);

                    await Task.WhenAll(coordinatesTask, adresseTask);

                    return new MotorizedTrafficProperty
                    {
                        MatrikkelNumber = martikkelNumber,
                        Coordinates = coordinatesTask.Result,
                        CoOwners = property.Owners,
                        Address = adresseTask.Result,
                        IsFritidsbolig = property.IsFritidsbolig,
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing property in GetMotorizedTrafficInformation");
                    return null;
                }
            });

            var properties = await Task.WhenAll(propertyTasks);
            result.Properties.AddRange(properties.Where(p => p != null));
            return result;
        }

        private string BuildMatrikkelNumber(string kommuneNr, string gardsNr, string bruksNr, string festeNr, string seksjonsNr)
        {
            var stringBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(kommuneNr))
                stringBuilder.Append(kommuneNr + "-");
            if (!string.IsNullOrEmpty(gardsNr))
                stringBuilder.Append($"{gardsNr}");
            if (!string.IsNullOrEmpty(bruksNr))
                stringBuilder.Append($"/{bruksNr}");
            if (!string.IsNullOrEmpty(festeNr))
                stringBuilder.Append($"/{festeNr}");
            if (!string.IsNullOrEmpty(seksjonsNr))
                stringBuilder.Append($"/{seksjonsNr}");

            return stringBuilder.ToString();
        }

        private async Task<List<Address>> GetAdresserForProperty(
            List<Address> addresses,
            string fallbackKommunenr,
            string fallbackGnr,
            string fallbackBnr,
            string fallbackFnr)
        {
            addresses = addresses?.Where(a => a != null).ToList();
            if (addresses == null || addresses.Count == 0)
                return new List<Address>();

            // Sometimes we get the matrikkelnummer instead of streetname,
            // so we check if the address is a matrikkel number first and split it up if it is
            const string matrikkelpattern = @"^\d+/\d+/\d+$";
            const string matrikkelpattern2 = @"^\d{4}-\d+/\d+/\d+$";

            var addressTasks = addresses.Select(async address =>
            {
                string kommunenr = null;
                string gnr = null;
                string bnr = null;
                string fnr = null;
                string streetAddress = address.Street;

                if (!string.IsNullOrEmpty(address.Street) && Regex.IsMatch(address.Street, matrikkelpattern))
                {
                    var parts = address.Street.Split('/');
                    gnr = parts[0];
                    bnr = parts[1];
                    fnr = parts[2];
                    streetAddress = null;
                }
                else if (!string.IsNullOrEmpty(address.Street) && Regex.IsMatch(address.Street, matrikkelpattern2))
                {
                    var parts = address.Street.Split('-', '/');
                    kommunenr = parts[0];
                    gnr = parts[1];
                    bnr = parts[2];
                    fnr = parts[3];
                    streetAddress = null; // Clear the street address since it's actually a matrikkel number
                }

                return await _geonorgeClient.SearchByMatrikkelNumber(
                    kommunenr ?? fallbackKommunenr,
                    gnr ?? fallbackGnr,
                    bnr ?? fallbackBnr,
                    fnr ?? fallbackFnr,
                    streetAddress,
                    address.PostalCode,
                    address.City);
            });

            var results = await Task.WhenAll(addressTasks);

            // Filter out duplicates — can happen after address lookup
            return results
                .Where(r => r?.Adresser != null)
                .SelectMany(r => r.Adresser)
                .Select(a => new Address
                {
                    Street = a.Adressetekst,
                    PostalCode = a.Postnummer,
                    City = a.Poststed,
                })
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
    }
}
