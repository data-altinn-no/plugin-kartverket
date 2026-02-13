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
        /// Latitude and Longitude Coordinates separated by comma
        /// </summary>
        [JsonProperty("Coordinates")]
        public string Coordinates { get; set; } // lat,long, TODO: check if there should be
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
        /// Share of ownership as f
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
        public PropertyData ProperyData { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("CoOwners")]
        public List<CoOwner> Owners { get; set; }
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
}
