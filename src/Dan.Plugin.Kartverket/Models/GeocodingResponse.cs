using System.Collections.Generic;

namespace Dan.Plugin.Kartverket.Models
{
    public class GeocodingResponse
    {
        public List<Feature> Features { get; set; }
    }

    public class Feature
    {
        public Geometry Geometry { get; set; }
    }

    public class Geometry
    {
        public List<double> Coordinates { get; set; }
    }
}
