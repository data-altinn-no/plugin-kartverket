using Dan.Plugin.Kartverket.Config;
using Kartverket.Matrikkel.AdresseService;
using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public static class GrunnbokHelpers
    {
        private static DateTime SNAPSHOT_VERSJON_DATO = new DateTime(9999, 1, 1, 0, 0, 0);
        public static BasicHttpBinding GetBasicHttpBinding()
        {
            long maxMessageSize = 2048000;
            BasicHttpBinding myBinding = new BasicHttpBinding();
            myBinding.Security.Mode = BasicHttpSecurityMode.Transport;
            myBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            myBinding.MaxReceivedMessageSize = maxMessageSize;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
            return myBinding;
        }

        public static void SetGrunnbokWSCredentials(ClientCredentials credentials, ApplicationSettings settings, string serviceContext)
        {
            if(serviceContext.ToUpper() == "DIGITALEHELGELAND")
            {
                credentials.UserName.UserName = settings.GrunnbokUser;//settings.DigitaleHelgeLandGrunnbokUser;
                credentials.UserName.Password = settings.GrunnbokPw;//settings.DigitaleHelgeLandGrunnbokPw;
            }
            if(serviceContext.ToUpper() == "EDUEDILIGENCE")
            {
                credentials.UserName.UserName = settings.GrunnbokEDueDiligenceUser;
                credentials.UserName.Password = settings.GrunnbokEDueDiligencePw;
            }
            if(serviceContext.ToUpper() == "OED"|| serviceContext.ToUpper() == "DIGITALDODSBO")
            {
                credentials.UserName.UserName = settings.OEDGrunnbokUser;
                credentials.UserName.Password = settings.OEDGrunnbokPw;
            }
            else
            {
                ArgumentException.ThrowIfNullOrEmpty(serviceContext, nameof(serviceContext));
            }
                
        }

        public static void SetMatrikkelWSCredentials(ClientCredentials credentials, ApplicationSettings settings, string serviceContext)
        {
            if(serviceContext.ToUpper() == "DIGITALEHELGLAND")
            {
                credentials.UserName.UserName = settings.DigitaleHelgeLandMatrikkelUser;
                credentials.UserName.Password = settings.DigitaleHelgeLandMatrikkelPw;
            }
            if(serviceContext.ToUpper() == "EDUEDILIGENCE")
            {
                credentials.UserName.UserName = settings.MatrikkelEDueDiligenceUser;
                credentials.UserName.Password = settings.MatrikkelEDueDiligencePw;
            }
            if(serviceContext.ToUpper() == "OED"|| serviceContext.ToUpper() == "DIGITALDODSBO")
            {
                credentials.UserName.UserName = settings.MatrikkelOEDUser;
                credentials.UserName.Password = settings.MatrikkelOEDPw;
            }
            else
            {
                ArgumentException.ThrowIfNullOrEmpty(serviceContext, nameof(serviceContext));
            }
            
        }

        public static TContext CreateGrunnbokContext<TContext, TTimestamp>(string serviceContext)
            where TContext : new()
            where TTimestamp : new()
        {
            var context = new TContext();
            var timestamp = new TTimestamp();

            dynamic ctx = context; // use dynamic only internally
            dynamic tstmp = timestamp;

            tstmp.timestamp = SNAPSHOT_VERSJON_DATO;

            ctx.locale = "no_578";
            ctx.clientIdentification = serviceContext;
            ctx.clientTraceInfo = serviceContext+"_1";
            ctx.systemVersion = "1";
            ctx.snapshotVersion = tstmp;

            return context;
        }

        public static TContext CreateMatrikkelContext<TContext, TTimestamp>()
            where TContext : new()
            where TTimestamp : new()
        {
            var context = new TContext();
            var timestamp = new TTimestamp();

            dynamic ctx = context; // use dynamic only internally
            dynamic tstmp = timestamp;

            tstmp.timestamp = SNAPSHOT_VERSJON_DATO;

            ctx.locale = "no_NO";
            ctx.brukOriginaleKoordinater = true;
            ctx.koordinatsystemKodeId = new KoordinatsystemKodeId()
            { 
                value = 22
            };
            ctx.klientIdentifikasjon = "eDueDiligence";
            ctx.snapshotVersion = tstmp;
            ctx.systemVersion = "trunk";

            return context;
        }
    }
    
}
