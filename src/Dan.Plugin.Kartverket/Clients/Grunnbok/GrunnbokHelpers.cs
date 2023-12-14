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

namespace Dan.Plugin.Kartverket.Clients.Grunnbok
{
    public static class GrunnbokHelpers
    {
        public static BasicHttpBinding GetBasicHttpBinding()
        {
            long maxMessageSize = 2048000;
            BasicHttpBinding myBinding = new BasicHttpBinding();
            myBinding.Security.Mode = BasicHttpSecurityMode.Transport;
            myBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            myBinding.MaxReceivedMessageSize = maxMessageSize;

            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
            return myBinding;
        }

        public static void SetGrunnbokWSCredentials(ClientCredentials credentials, ApplicationSettings settings)
        {
            credentials.UserName.UserName = settings.GrunnbokUser;
            credentials.UserName.Password = settings.GrunnbokPw;
        }

        public static void SetMatrikkelWSCredentials(ClientCredentials credentials, ApplicationSettings settings)
        {
            credentials.UserName.UserName = settings.MatrikkelUser;
            credentials.UserName.Password = settings.MatrikkelPw;
        }

    }
}
