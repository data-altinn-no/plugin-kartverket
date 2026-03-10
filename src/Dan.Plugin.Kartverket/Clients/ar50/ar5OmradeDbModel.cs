using System;

namespace Dan.Plugin.Kartverket.Clients.ar50
{
    public class Ar5OmradeDbModel
    {
        public int Objectid { get; set; }
        public string ArealType { get; set; }
        public double ShapeLength { get; set; }
        public double ShapeArea { get; set; }
        public string Shape { get; set; }
    }
}
