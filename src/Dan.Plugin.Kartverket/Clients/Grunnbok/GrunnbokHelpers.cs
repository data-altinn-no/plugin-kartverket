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
            credentials.UserName.UserName = settings.GrunnbokUser2;
            credentials.UserName.Password = settings.GrunnbokPw2;
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

    public enum ServiceContext
    {
        Grunnbok,       
        Matrikkel
    }   
}
