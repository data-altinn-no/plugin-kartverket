using System;

namespace Dan.Plugin.Kartverket.Config
{
    public class ApplicationSettings
    {
        public TimeSpan BreakerRetryWaitTime { get; set; }
        public bool IsTestEnv { get; set; }
        public string KartverketRegisterenhetsrettsandelerForPersonUrl { get; set; }
        public string KartverketRettigheterForPersonUrl { get; set; }
        public string LandbrukUrl { get; set; }
        public string AddressLookupUrl { get; set; }
    }
}
