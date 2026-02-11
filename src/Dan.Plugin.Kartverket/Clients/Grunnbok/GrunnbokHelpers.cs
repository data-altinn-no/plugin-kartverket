using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Description;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Dan.Plugin.Kartverket.Config;
using Kartverket.Grunnbok.StoreService;
using Dan.Common.Models;
using Kartverket.Matrikkel.AdresseService;

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

        public static void SetCredentials(ClientCredentials credentials, ApplicationSettings settings, ServiceContext serviceContext)
        {
            switch(serviceContext)
            {
                case ServiceContext.Grunnbok:
                    credentials.UserName.UserName = settings.GrunnbokUser;
                    credentials.UserName.Password = settings.GrunnbokPw;
                    break;
                case ServiceContext.Matrikkel:
                    credentials.UserName.UserName = settings.MatrikkelUser;
                    credentials.UserName.Password = settings.MatrikkelPw;
                    break;
                default:
                    throw new ArgumentException($"Unsupported service context: {serviceContext}");
            }
        }

        

        public static MatrikkelContext GetMatrikkelContext()
        {
            return new MatrikkelContext()
            {
                locale = "no_NO",
                brukOriginaleKoordinater = true,
                koordinatsystemKodeId = new KoordinatsystemKodeId()
                {
                    value = 22
                },
                klientIdentifikasjon = "eDueDiligence",
                snapshotVersion = new ()
                {
                    timestamp = SNAPSHOT_VERSJON_DATO
                },
                systemVersion = "trunk"
            };
        }

        public static TContext CreateGrunnbokContext<TContext, TTimestamp>()
            where TContext : new()
            where TTimestamp : new()
        {
            var context = new TContext();
            var timestamp = new TTimestamp();

            dynamic ctx = context; // use dynamic only internally
            dynamic tstmp = timestamp;

            tstmp.timestamp = new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Local);

            ctx.locale = "no_578";
            ctx.clientIdentification = "eDueDiligence";
            ctx.clientTraceInfo = "eDueDiligence_1";
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

            tstmp.timestamp = new DateTime(9999, 1, 1, 0, 0, 0, DateTimeKind.Local);

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



    public enum ServiceContext
    {
        Grunnbok,       
        Matrikkel
    }   
}
