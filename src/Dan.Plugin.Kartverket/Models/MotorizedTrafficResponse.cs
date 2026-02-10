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

        public string MatrikkelNumber { get; set; } 

        /// <summary>
        /// CoOwners of the property
        /// </summary>
        public List<CoOwner> CoOwners { get; set; } = new();
        /// <summary>
        /// Latitude and Longitude Coordinates separated by comma
        /// </summary>

        public string Coordinates { get; set; } // lat,long, TODO: check if there should be
    }

    public class CoOwner {
        /// <summary>
        /// Organization number or ssn
        /// </summary>
        public string Identifier { get; set; }
        /// <summary>
        /// Name of person or organization
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Share of ownership as f
        /// </summary>
        public string OwnerShare { get; set; }
    }

    public class  PropertyWithOwners
    {
        [JsonProperty("grunnboksinformasjon")]
        public PropertyData ProperyData { get; set; }

        [JsonProperty("rettighetshavereTilEiendomsrett")]
        public List<CoOwner> Owners { get; set; }

    }

    public class PropertyData
    {
        public string Kommunenummer { get; set; }
        public string Komunnenavn { get; set; }
        public string Gardsnummer { get; set; }
        public string Bruksnummer { get; set; }
        public string Festenummer { get; set; }
        public string Seksjonsnummer { get; set; }
    }
}
