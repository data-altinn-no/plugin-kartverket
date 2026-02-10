using System;

namespace Dan.Plugin.Kartverket.Config
{
    public class ApplicationSettings
    {
        public TimeSpan BreakerRetryWaitTime { get; set; }
        public bool IsTestEnv { get; set; }
        public string KartverketRegisterenhetsrettsandelerForPersonUrl { get; set; }
        public string KartverketRettigheterForPersonUrl { get; set; }

        public string KartverketAdresseForBorettslagsandelUrl { get; set; }
        public string LandbrukUrl { get; set; }
        public string AddressLookupUrl { get; set; }

        public string MatrikkelPw { get; set; }

        public string MatrikkelUser { get; set; }

        public string GrunnbokUser { get; set; }

        public string GrunnbokPw { get; set; }
        public string GrunnbokUser2 { get; set; }
        public string GrunnbokPw2 { get; set; } 

        public string GrunnbokRootUrl { get; set; }
        public string GrunnbokBaseUrl { get; set; }

        public string MatrikkelRootUrl { get; set; }

        public string CoordinatesLookupUrl { get; set; }
    }
}
