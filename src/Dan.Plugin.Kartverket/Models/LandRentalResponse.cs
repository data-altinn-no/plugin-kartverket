using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Models
{
    public class LandRentalResponse
    {
        /// <summary>
        /// Matrikkelnumber
        /// </summary>
        public string Matrikkelnumber { get; set; }
        /// <summary>
        /// Jordtype - Type of dirt/land
        /// </summary>
        public List<JordType> JordType { get; set; }

    }

    public class JordType
    {
        public int FeatureId { get; set; }
        public ArealType ArealType { get; set; }
        public double Areal { get; set; }
        public string GeoJson { get; set; }
    }

    public enum ArealType
    {
        //Documentation for
        //the different types:
        //https://www.nibio.no/tjenester/nedlasting-av-kartdata/dokumentasjon/fkb-ar5
        Bebygd = 11,
        Samferdsel = 12,
        FulldyrkaJord = 21,
        OverflateDyrkaJord = 22,
        Innmarksbeite = 23,
        Skog = 30,
        ApenFastmark = 50,
        Myr = 60,
        Bre = 70,
        Ferskvann = 81,
        Hav = 82,
        IkkeKartlagt = 99
    }
}
