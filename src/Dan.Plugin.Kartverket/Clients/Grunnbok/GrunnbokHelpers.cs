using Dan.Plugin.Kartverket.Clients.Grunnbok.GrunnbokContextHelpers;
using Dan.Plugin.Kartverket.Clients.Matrikkel.MatrikkelContextHelpers;
using Dan.Plugin.Kartverket.Config;
using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using IGrunnbokSnapshotTimestamp = Dan.Plugin.Kartverket.Clients.Grunnbok.GrunnbokContextHelpers.ISnapshotTimestamp;
using IMatrikkelSnapshotTimestamp = Dan.Plugin.Kartverket.Clients.Matrikkel.MatrikkelContextHelpers.ISnapshotTimestamp;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public static class GrunnbokHelpers
    {
        private static readonly DateTime SNAPSHOT_VERSJON_DATO = new DateTime(9999, 1, 1, 0, 0, 0);
        public static BasicHttpBinding GetBasicHttpBinding()
        {
            BasicHttpBinding myBinding = new BasicHttpBinding();
            myBinding.Security.Mode = BasicHttpSecurityMode.Transport;
            myBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            myBinding.MaxReceivedMessageSize = int.MaxValue;
            myBinding.MaxBufferSize = int.MaxValue;
            myBinding.SendTimeout = TimeSpan.FromSeconds(30);
            myBinding.ReceiveTimeout = TimeSpan.FromSeconds(30);
            myBinding.OpenTimeout = TimeSpan.FromSeconds(30);
            // Without an explicit CloseTimeout, closing a slow/half-open channel can block
            // the request thread for the WCF default of 1 minute - far past the 30s above.
            myBinding.CloseTimeout = TimeSpan.FromSeconds(30);

            return myBinding;
        }

        public static void SetGrunnbokWSCredentials(ClientCredentials credentials, ApplicationSettings settings, string serviceContext)
        {
            ArgumentException.ThrowIfNullOrEmpty(serviceContext, nameof(serviceContext));

            if (serviceContext.ToUpper() == "DIGITALEHELGELAND")
            {
                credentials.UserName.UserName = settings.GrunnbokUserDigitaleHelgeland;
                credentials.UserName.Password = settings.GrunnbokPwDigitaleHelgeland;
            }
            else if(serviceContext.ToUpper() == "EDUEDILIGENCE")
            {
                credentials.UserName.UserName = settings.GrunnbokEDueDiligenceUser;
                credentials.UserName.Password = settings.GrunnbokEDueDiligencePw;
            }
            else if(serviceContext.ToUpper() == "OED"|| serviceContext.ToUpper() == "DIGITALDODSBO")
            {
                credentials.UserName.UserName = settings.GrunnbokUser;
                credentials.UserName.Password = settings.GrunnbokPw;
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
                credentials.UserName.UserName = settings.MatrikkelUserDigitaleHelgeland;
                credentials.UserName.Password = settings.MatrikkelPwDigitaleHelgeland;
            }
            else if(serviceContext.ToUpper() == "EDUEDILIGENCE")
            {
                credentials.UserName.UserName = settings.MatrikkelEDueDiligenceUser;
                credentials.UserName.Password = settings.MatrikkelEDueDiligencePw;
            }
            else if(serviceContext.ToUpper() == "OED"|| serviceContext.ToUpper() == "DIGITALDODSBO")
            {
                credentials.UserName.UserName = settings.MatrikkelUser;
                credentials.UserName.Password = settings.MatrikkelPw;
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

            timestamp.timestamp = SNAPSHOT_VERSJON_DATO;

            context.locale = "no_578";
            context.clientIdentification = serviceContext;
            context.clientTraceInfo = serviceContext+"_1";
            context.systemVersion = "1";
            context.snapshotVersion = timestamp;

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


