using Dan.Plugin.Kartverket.Models;
using System;

namespace Dan.Plugin.Kartverket.Clients.ar50
{
    public class Ar5OmradeDbModel
    {
        public int Objectid { get; set; }
        public ArealType ArealType { get; set; }
        public double ClippedArea { get; set; }
        public string GeoJson { get; set; }
    }
}
