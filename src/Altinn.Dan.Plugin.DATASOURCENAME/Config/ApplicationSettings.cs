using System;
using System.Security.Cryptography.X509Certificates;

namespace Altinn.Dan.Plugin.DATASOURCENAME.Config
{
    public class ApplicationSettings
    {
        public string RedisConnectionString { get; set; }
        public TimeSpan BreakerRetryWaitTime { get; set; }
        public string DATASETNAME1URL { get; set; }
        public string DATASETNAME2URL { get; set; }
        public X509Certificate Certificate { get; set; }
    }
}
