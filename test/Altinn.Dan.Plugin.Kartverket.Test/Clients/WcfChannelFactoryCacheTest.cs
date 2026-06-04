using AwesomeAssertions;
using Dan.Plugin.Kartverket.Clients;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using Xunit;

namespace Dan.Plugin.Kartverket.Test.Clients
{
    [ServiceContract]
    public interface IDummyWcfService
    {
        [OperationContract]
        Task<string> EchoAsync(string input);
    }

    public class WcfChannelFactoryCacheTest
    {
        private static BasicHttpBinding CreateBinding()
        {
            return new BasicHttpBinding
            {
                Security =
                {
                    Mode = BasicHttpSecurityMode.Transport,
                    Transport = { ClientCredentialType = HttpClientCredentialType.Basic }
                }
            };
        }

        [Fact]
        public void SameCacheKey_ReusesFactory_AndAppliesCredentialsOnlyOnce()
        {
            var endpoint = new EndpointAddress($"https://example.test/{Guid.NewGuid()}/ServiceWS");
            var cacheKey = $"{endpoint.Uri}|OED";
            var applications = 0;

            var channel1 = WcfChannelFactoryCache<IDummyWcfService>.CreateChannel(
                cacheKey, endpoint, CreateBinding(), _ => applications++);
            var channel2 = WcfChannelFactoryCache<IDummyWcfService>.CreateChannel(
                cacheKey, endpoint, CreateBinding(), _ => applications++);

            // Both calls must yield usable channels, but the factory (and its credentials)
            // is only built once for a given (endpoint, serviceContext) key
            channel1.Should().NotBeNull();
            channel2.Should().NotBeNull();
            applications.Should().Be(1);

            ((ICommunicationObject)channel1).Abort();
            ((ICommunicationObject)channel2).Abort();
        }

        [Fact]
        public void DifferentServiceContexts_GetSeparateFactories_WithTheirOwnCredentials()
        {
            var endpoint = new EndpointAddress($"https://example.test/{Guid.NewGuid()}/ServiceWS");
            var appliedCredentials = new List<(string key, ClientCredentials credentials)>();

            var oedChannel = WcfChannelFactoryCache<IDummyWcfService>.CreateChannel(
                $"{endpoint.Uri}|OED", endpoint, CreateBinding(), credentials =>
                {
                    credentials.UserName.UserName = "oed-user";
                    appliedCredentials.Add(("OED", credentials));
                });

            var helgelandChannel = WcfChannelFactoryCache<IDummyWcfService>.CreateChannel(
                $"{endpoint.Uri}|DIGITALEHELGELAND", endpoint, CreateBinding(), credentials =>
                {
                    credentials.UserName.UserName = "helgeland-user";
                    appliedCredentials.Add(("DIGITALEHELGELAND", credentials));
                });

            // Each service context must get its own factory with its own credentials —
            // a shared factory here would mean calling Kartverket with the wrong user
            appliedCredentials.Should().HaveCount(2);
            appliedCredentials[0].credentials.Should().NotBeSameAs(appliedCredentials[1].credentials);
            appliedCredentials[0].credentials.UserName.UserName.Should().Be("oed-user");
            appliedCredentials[1].credentials.UserName.UserName.Should().Be("helgeland-user");

            ((ICommunicationObject)oedChannel).Abort();
            ((ICommunicationObject)helgelandChannel).Abort();
        }

        [Fact]
        public void DifferentEndpoints_SameServiceContext_GetSeparateFactories()
        {
            var endpointA = new EndpointAddress($"https://example.test/{Guid.NewGuid()}/AServiceWS");
            var endpointB = new EndpointAddress($"https://example.test/{Guid.NewGuid()}/BServiceWS");
            var applications = 0;

            var channelA = WcfChannelFactoryCache<IDummyWcfService>.CreateChannel(
                $"{endpointA.Uri}|OED", endpointA, CreateBinding(), _ => applications++);
            var channelB = WcfChannelFactoryCache<IDummyWcfService>.CreateChannel(
                $"{endpointB.Uri}|OED", endpointB, CreateBinding(), _ => applications++);

            applications.Should().Be(2);

            ((ICommunicationObject)channelA).Abort();
            ((ICommunicationObject)channelB).Abort();
        }
    }
}
