using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Dan.Plugin.Kartverket.Models
{
    public class PropertyModel
    {
        [JsonProperty("grunnboksinformasjon")]
        public GrunnboksInformasjon Grunnbok { get; set; }

        [JsonProperty("rettighetshavereTilEiendomsrett")]
        public Rettighetshavere Owners { get; set; }

        [JsonProperty("pantedokumenter")]
        public List<PawnDocument> Documents { get; set; }

        [JsonProperty("harKulturminne")]
        public bool HasCulturalHeritageSite { get; set; }
    }

    public class GrunnboksInformasjon
    {
        [JsonProperty("kommune")]
        public string CountyMunicipality { get; set; }
        [JsonProperty("gaardsnummer")]
        public string gnr { get; set; }

        [JsonProperty("bruksnummer")]
        public string bnr { get; set; }

        [JsonProperty("bygningsareal")]
        public double BuildingArea { get; set; }

        [JsonProperty("teigarealer")]
        public List<double> TeigAreas { get; set; }
    }

    public class Rettighetshavere
    {
        [JsonProperty("datoHjemmelEiendomsrett")]
        public DateTime? EstablishedDate { get; set; }
        [JsonProperty("vederlag")]
        public string Price { get; set; }
        [JsonProperty("eierandel")]
        public string Share { get; set; }
    }

    
    public class PawnDocument
    {
        [JsonProperty("beloep")]
        public List<Amount> Amounts { get; set; }
        [JsonProperty("pantehaver")]
        public string Owner { get; set; }

        [JsonIgnore]
        public long OwnerId { get; set; }
    }

    public class OwnerShipTransferInfo
    {
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; }

        public DateTime? EstablishedDate { get; set; }
    }

    public class Amount
    {
        [JsonProperty("grunnboksinformasjon")]
        public decimal Sum { get; set; }

        [JsonProperty("valuta")]
        public string CurrencyCode { get; set; }

        [JsonProperty("beloeptekst", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AmountText { get; set; }
    }

    public class Kommune
    {
        public string Name { get; set; }

        public string Number { get; set; }
    }

    public class MatrikkelEnhetMedteig
    {
        public List<double> Teiger { get; set; }

        public string Gaardsnummer { get; set; }

        public string Bruksnummer { get; set; }

        public bool HasCulturalHeritageSite { get; set; }
    }

 

    /*
     * Grunnboksinformasjon
Kommune
Gnr / Bnr
Areal: X XXX,XX m²
Rettighetshavere til eiendomsrett
Dato – Hjemmel til eiendomsrett
Vederlag: NOK XX XXX XXX
Virksomhetens organisasjonsnummer
Virksomhetens navn
Pantedokument
Beløp
Panthaver
Kulturminner
Ja/Nei
     */
}
