using NetTopologySuite.Geometries;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
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
        public string ArealType { get; set; }
        public double Areal { get; set; }
        public string GeoJson { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ArealType
    {
        //Documentation for
        //the different types:
        //https://www.nibio.no/tjenester/nedlasting-av-kartdata/dokumentasjon/fkb-ar5
        [EnumMember(Value = "Bebygd")]
        Bebygd = 11,
        [EnumMember(Value = "Samferdsel")]
        Samferdsel = 12,
        [EnumMember(Value = "FulldyrkaJord")]
        FulldyrkaJord = 21,
        [EnumMember(Value = "OverflateDyrkaJord")]
        OverflateDyrkaJord = 22,
        [EnumMember(Value = "Innmarksbeite")]
        Innmarksbeite = 23,
        [EnumMember(Value = "Skog")]
        Skog = 30,
        [EnumMember(Value = "ApenFastmark")]
        ApenFastmark = 50,
        [EnumMember(Value = "Myr")]
        Myr = 60,
        [EnumMember(Value = "Bre")]
        Bre = 70,
        [EnumMember(Value = "Ferskvann")]
        Ferskvann = 81,
        [EnumMember(Value = "Hav")]
        Hav = 82,
        [EnumMember(Value = "IkkeKartlagt")]
        IkkeKartlagt = 99
    }
}
