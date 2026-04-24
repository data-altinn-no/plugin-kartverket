using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dan.Plugin.Kartverket.Models
{
    public class GeocodingResponse
    {
        public List<Feature> Features { get; set; }
    }

    public class Feature
    {
        public Geometry Geometry { get; set; }
        public Properties Properties { get; set; }
    }

    public class Geometry
    {        
        public JsonElement Coordinates { get; set; }        
    }

    public class Properties
    {
        [JsonPropertyName("hovedområde")]
        public bool HovedOmrade { get; set; }
    }

}
