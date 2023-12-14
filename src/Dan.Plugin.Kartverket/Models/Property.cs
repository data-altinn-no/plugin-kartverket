using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Models
{
    public class PropertyModel
    {
        public GrunnboksInformasjon Grunnbok { get; set; }

        public Rettighetshavere Owners { get; set; }

        public PawnDocument Documents { get; set; }

        public bool HasCulturalHeritageSite { get; set; }
    }

    public class GrunnboksInformasjon
    {
        public string CountyMunicipality { get; set; }
        public string gnr { get; set; }

        public string bnr { get; set; }
        public double Area { get; set; }
    }

    public class Rettighetshavere
    {
        public DateTime EstablishedDate { get; set; }
        public double Price { get; set; }
        public string OrgNo { get; set; }
        public string OrgName { get; set; }
    }

    public class PawnDocument
    {
        public double Amount { get; set; }
        public string Owner { get; set; }
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
