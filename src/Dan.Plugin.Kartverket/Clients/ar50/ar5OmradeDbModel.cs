using System;
using NetTopologySuite.Geometries;

namespace Dan.Plugin.Kartverket.Clients.ar50
{
    public class ar5OmradeDbModel
    {
        public int Objectid { get; set; }
        public string Objtype { get; set; }
        public string Lokalid { get; set; }
        public DateTimeOffset Datafangstdato { get; set; }
        public DateTimeOffset Verifiseringsdato { get; set; }
        public DateTimeOffset Oppdateringsdato { get; set; }
        public string Informasjon { get; set; }
        public DateTimeOffset Sluttdato { get; set; }
        public string Registreringsversjon { get; set; }
        public string Navnerom { get; set; }
        public string Versjonid { get; set; }
        public string Opphav { get; set; }
        public string ArealType { get; set; }
        public string Treslag { get; set; }
        public string Skogbonitet { get; set; }
        public string Grunnforhold { get; set; }
        public string Klassifiseringsmetode { get; set; }
        public double ShapeLength { get; set; }
        public double ShapeArea { get; set; }
        public string Shape { get; set; }
    }
}
