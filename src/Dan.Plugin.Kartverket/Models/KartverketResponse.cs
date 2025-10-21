using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dan.Plugin.Kartverket.Models
{
    public class KartverketResponse
    {
        public PropertyRights PropertyRights { get; set; }
    }

    public class PropertyRights
    {
        public IEnumerable<Property> Properties { get; set; }

        public IEnumerable<PropertyWithRights> PropertiesWithRights { get; set; }
    }

    public class Property
    {
        public string Address { get; set; }

        public List<string> AddressList { get; set; }

        public string City { get; set; }

        public string PostalCode { get; set; }

        // Eiendomsrett / Framfesterett (1,2,3) / Veirett / Borett / Leierett
        public string Type { get; set; }

        // Kommunenavn
        public string Municipality { get; set; }

        // Kommunenummer
        public string MunicipalityNumber { get; set; }

        // Gårdsnummer
        public string HoldingNumber { get; set; }

        // Bruksnummer
        public string SubholdingNumber { get; set; }

        // Festenummer
        public string LeaseNumber { get; set; }

        // Seksjonsnummer
        public string SectionNumber { get; set; }

        // Eierandel i brøk
        public string FractionOwnership { get; set; }

        // Is added from the "landbruk" integration
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public bool IsAgriculture { get; set; }
    }

    public class PropertyWithRights : Property
    {
        public List<Right> Rights { get; set; }
    }

    public class Right
    {
        public string DocumentYear { get; set; }

        public string DocumentNumber { get; set; }

        public string OfficeNumber { get; set; }

        public string JudgementNumber { get; set; }

        public string JudgmentType { get; set; }
    }
}
