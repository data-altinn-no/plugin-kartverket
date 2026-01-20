using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Models
{
    public class MotorizedTrafficResponse
    {
        public List<MotorizedTrafficProperty> Properties = new();
    }

    public class MotorizedTrafficProperty {

        public string MatrikkelNumber { get; set; } //0301-223/60/0/3 - kommune-gnr/bnr/festenr/seksjonsnr

        public List<CoOwner> CoOwners { get; set; } = new();

        public string Coordinates { get; set; } // lat,long, TODO: check if there should be 
    }

    public class CoOwner {
        public string Identifier { get; set; }
        public string Name { get; set; }

        public string OwnerShare { get; set; } // as fraction
    }
}   
