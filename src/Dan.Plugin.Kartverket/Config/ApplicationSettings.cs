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

        public string DigitaleHelgeLandGrunnbokUser { get; set; }
        public string DigitaleHelgeLandGrunnbokPw { get; set; }
        public string GrunnbokEDueDiligenceUser { get; set; }
        public string GrunnbokEDueDiligencePw { get; set; }
        public string OEDGrunnbokUser { get; set; }
        public string OEDGrunnbokPw { get; set; }

        public string GrunnbokRootUrl { get; set; }

        public string MatrikkelRootUrl { get; set; }
        public string DigitaleHelgeLandMatrikkelUser { get; set; }
        public string DigitaleHelgeLandMatrikkelPw { get; set; }
        public string MatrikkelEDueDiligenceUser { get; set; }
        public string MatrikkelEDueDiligencePw { get; set; }
        public string MatrikkelOEDUser { get; set; }
        public string MatrikkelOEDPw { get; set; }

        public string CoordinatesLookupUrl { get; set; }
    }
}
