using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dan.Plugin.Kartverket.Models
{
    public class MotorizedTrafficResponse
    {
        public List<MotorizedTrafficProperty> Properties = new();
    }

    public class MotorizedTrafficProperty {
        /// <summary>
        /// Matrikkel number of the property
        /// </summary>
        [JsonProperty("Matrikkelnumber")]
        public string MatrikkelNumber { get; set; }

        /// <summary>
        /// CoOwners of the property
        /// </summary>
        [JsonProperty("CoOwners")]
        public List<CoOwner> CoOwners { get; set; } = new();
        /// <summary>
        /// Latitude and Longitude Coordinates
        /// </summary>
        [JsonProperty("Coordinates")]
        public List<List<double>> Coordinates { get; set; }
        /// <summary>
        /// Address of property
        /// </summary>
        [JsonProperty("Address")]
        public Address Address { get; set; }
        /// <summary>
        /// If the property is a fritidsbolig or not
        /// </summary>
        [JsonProperty("IsFritidsbolig")]
        public bool IsFritidsbolig { get; set; }
    }

    public class CoOwner {
        /// <summary>
        /// Organization number or ssn
        /// </summary>
        [JsonProperty("IdentificationNumber")]
        public string Identifier { get; set; }
        /// <summary>
        /// Name of person or organization
        /// </summary>
        [JsonProperty("Name")]
        public string Name { get; set; }
        /// <summary>
        /// Share of ownership as fraction
        /// </summary>
        [JsonProperty("OwnerShare")]
        public string OwnerShare { get; set; }
    }

    public class  PropertyWithOwners
    {
        /// <summary>
        /// Data about property. From Grunnboka
        /// </summary>
        [JsonProperty("PropertyData")]
        public PropertyData PropertyData { get; set; }
        /// <summary>
        /// All current owners of the property. From Grunnboka
        /// </summary>
        [JsonProperty("CoOwners")]
        public List<CoOwner> Owners { get; set; }
        /// <summary>
        /// Property address. From Geonorge
        /// </summary>
        [JsonProperty("Address")]
        public Address Address { get; set; }
        public bool IsFritidsbolig { get; set; }
    }

    public class PropertyData
    {
        [JsonProperty("Kommunenummer")]
        public string Kommunenummer { get; set; }
        [JsonProperty("Kommunenavn")]
        public string Kommunenavn { get; set; }
        [JsonProperty("Gardsnummer")]
        public string Gardsnummer { get; set; }
        [JsonProperty("Bruksnummer")]
        public string Bruksnummer { get; set; }
        [JsonProperty("Festenummer")]
        public string Festenummer { get; set; }
        [JsonProperty("Seksjonsnummer")]
        public string Seksjonsnummer { get; set; }
    }

    public class Address
    {
        [JsonProperty("Street")]
        public string Street { get; set; }
        [JsonProperty("PostalCode")]
        public string PostalCode { get; set; }
        [JsonProperty("City")]
        public string City { get; set; }
    }
}
