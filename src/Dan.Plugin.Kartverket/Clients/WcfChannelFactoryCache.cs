using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients
{
    /// <summary>
    /// Caches one <see cref="ChannelFactory{TChannel}"/> per (endpoint, credentials) combination.
    ///
    /// The generated *ServiceClient classes build a brand new ChannelFactory on every call. On .NET
    /// each ChannelFactory owns its own HttpClient and connection pool, so a new factory per call
    /// means a fresh TLS handshake and zero connection reuse. Under the heavy Task.WhenAll fan-out in
    /// KartverketGrunnbokMatrikkelService that produced connection churn, exhaustion and the 30s
    /// SendTimeout firing. Caching the factory lets the HTTP layer pool and reuse connections while a
    /// cheap, short-lived channel is still created per call.
    ///
    /// Credentials live on the factory, not the channel, so the cache key must include the service
    /// context (which selects the username/password). Callers upper-case the context in the key to
    /// match the case-insensitive credential selection, so differently-cased spellings of the same
    /// context share one factory. The set of (endpoint, context) pairs is small and bounded.
    /// </summary>
    public static class WcfChannelFactoryCache<TChannel>
    {
        private static readonly ConcurrentDictionary<string, Lazy<ChannelFactory<TChannel>>> Factories = new();

        public static TChannel CreateChannel(
            string cacheKey,
            EndpointAddress endpoint,
            Binding binding,
            Action<ClientCredentials> applyCredentials)
        {
            while (true)
            {
                var lazy = Factories.GetOrAdd(cacheKey, _ => new Lazy<ChannelFactory<TChannel>>(() =>
                {
                    var factory = new ChannelFactory<TChannel>(binding, endpoint);
                    applyCredentials(factory.Credentials);
                    return factory;
                }));

                var factory = lazy.Value;

                // A faulted factory can never recover; evict it and rebuild on the next iteration.
                if (factory.State == CommunicationState.Faulted)
                {
                    if (Factories.TryRemove(new KeyValuePair<string, Lazy<ChannelFactory<TChannel>>>(cacheKey, lazy)))
                    {
                        try { factory.Abort(); } catch { /* best effort */ }
                    }

                    continue;
                }

                return factory.CreateChannel();
            }
        }
    }

    public static class WcfChannelExtensions
    {
        /// <summary>
        /// Closes a per-call channel without blocking the request thread, aborting if the graceful
        /// close fails (for example when the channel has already faulted on a timeout).
        /// </summary>
        public static async Task CloseChannelAsync(this IClientChannel channel)
        {
            if (channel == null)
            {
                return;
            }

            try
            {
                await Task.Factory.FromAsync(channel.BeginClose, channel.EndClose, null).ConfigureAwait(false);
            }
            catch
            {
                channel.Abort();
            }
        }
    }
}
