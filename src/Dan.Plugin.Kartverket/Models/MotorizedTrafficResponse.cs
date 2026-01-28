using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dan.Plugin.Kartverket.Models
{
    public class MotorizedTrafficResponse
    {
        public List<MotorizedTrafficProperty> Properties = new();
    }

    public class MotorizedTrafficProperty {

        public string MatrikkelNumber { get; set; } //0301-223/60/0/3 - kommune-gnr/bnr/festenr/seksjonsnr

        public List<CoOwner> CoOwners { get; set; } = new();

        public string Coordinates { get; set; } // lat,long, TODO: check if there should be
    }

    public class CoOwner {
        public string Identifier { get; set; }
        public string Name { get; set; }

        public string OwnerShare { get; set; } // as fraction
    }

    public class  PropertyEkstra
    {
        [JsonProperty("grunnboksinformasjon")]
        public EkstraGrunnbokdata Grunnbok { get; set; }

        [JsonProperty("rettighetshavereTilEiendomsrett")]
        public Rettighetshavere Owners { get; set; }

        [JsonProperty("pantedokumenter")]
        public List<PawnDocument> Documents { get; set; }

        [JsonProperty("harKulturminne")]
        public bool HasCulturalHeritageSite { get; set; }
    }

    public class EkstraGrunnbokdata
    {
        public string Kommunenummer { get; set; }
        public string CountyMunicipality { get; set; }
        public string Gardsnummer { get; set; }

        public string Bruksnummer { get; set; }
        public string Festenummer { get; set; }
        public string Seksjonsnummer { get; set; }
    }
}
