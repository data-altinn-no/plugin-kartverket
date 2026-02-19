using Dan.Plugin.Kartverket.Clients.Grunnbok.GrunnbokContextHelpers;
using Dan.Plugin.Kartverket.Clients.Matrikkel.MatrikkelContextHelpers;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Matrikkel.AdresseService;
using System;
using System.Data;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using IGrunnbokSnapshotTimestamp = Dan.Plugin.Kartverket.Clients.Grunnbok.GrunnbokContextHelpers.ISnapshotTimestamp;
using IMatrikkelSnapshotTimestamp = Dan.Plugin.Kartverket.Clients.Matrikkel.MatrikkelContextHelpers.ISnapshotTimestamp;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public static class GrunnbokHelpers
    {
        private static readonly DateTime SNAPSHOT_VERSJON_DATO = new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Local);
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
            ArgumentException.ThrowIfNullOrEmpty(serviceContext, nameof(serviceContext));

            if (serviceContext.ToUpper() == "DIGITALEHELGELAND")
            {
                credentials.UserName.UserName = settings.DigitaleHelgeLandGrunnbokUser;
                credentials.UserName.Password = settings.DigitaleHelgeLandGrunnbokPw;
            }
            else if(serviceContext.ToUpper() == "EDUEDILIGENCE")
            {
                credentials.UserName.UserName = settings.GrunnbokEDueDiligenceUser;
                credentials.UserName.Password = settings.GrunnbokEDueDiligencePw;
            }
            else if(serviceContext.ToUpper() == "OED"|| serviceContext.ToUpper() == "DIGITALDODSBO")
            {
                credentials.UserName.UserName = settings.OEDGrunnbokUser;
                credentials.UserName.Password = settings.OEDGrunnbokPw;
            }
            else
            {
                throw new ArgumentException("Invalid service context", nameof(serviceContext));
            }
                
        }

        public static void SetMatrikkelWSCredentials(ClientCredentials credentials, ApplicationSettings settings, string serviceContext)
        {
            ArgumentException.ThrowIfNullOrEmpty(serviceContext, nameof(serviceContext));

            if (serviceContext.ToUpper() == "DIGITALEHELGELAND")
            {
                credentials.UserName.UserName = settings.DigitaleHelgeLandMatrikkelUser;
                credentials.UserName.Password = settings.DigitaleHelgeLandMatrikkelPw;
            }
            else if(serviceContext.ToUpper() == "EDUEDILIGENCE")
            {
                credentials.UserName.UserName = settings.MatrikkelEDueDiligenceUser;
                credentials.UserName.Password = settings.MatrikkelEDueDiligencePw;
            }
            else if(serviceContext.ToUpper() == "OED"|| serviceContext.ToUpper() == "DIGITALDODSBO")
            {
                credentials.UserName.UserName = settings.MatrikkelOEDUser;
                credentials.UserName.Password = settings.MatrikkelOEDPw;
            }
            else
            {
                throw new ArgumentException("Invalid service context", nameof(serviceContext));
            }
            
        }

        public static TContext CreateGrunnbokContext<TContext, TTimestamp>(string serviceContext)
            where TContext : IGrunnbokContext<TTimestamp>, new()
            where TTimestamp : IGrunnbokSnapshotTimestamp, new()
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

        public static TContext CreateMatrikkelContext<TContext, TTimestamp, TKoordinatsystemKodeId>(
            string serviceContext)
            where TContext :IMatrikkelContext<TTimestamp, TKoordinatsystemKodeId>, new()
            where TTimestamp : IMatrikkelSnapshotTimestamp, new()
            where TKoordinatsystemKodeId : IKoordinatsystemKodeId, new()
        {
            var context = new TContext();
            var snapshot = new TTimestamp();
            var koordinatsystem = new TKoordinatsystemKodeId();

            snapshot.timestamp = SNAPSHOT_VERSJON_DATO;

            koordinatsystem.value = 22;

            context.locale = "no_NO";
            context.brukOriginaleKoordinater = true;
            context.koordinatsystemKodeId = koordinatsystem;
            context.klientIdentifikasjon = serviceContext;
            context.snapshotVersion = snapshot;
            context.systemVersion = "trunk";

            return context;
        }


    }


}


