using Azure.Core;
using Dan.Common.Models;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients
{
    public class RequestContextService : IRequestContextService
    {
        public const string ServicecontextHeader = "X-NADOBE-SERVICECONTEXT";

        public string ServiceContext { get; set; }

        public async Task SetRequestContext(HttpRequestData data)
        {
            ServiceContext = GetServiceContextFromRequest(data);
        }

        private string GetServiceContextFromRequest(HttpRequestData data)
        {
            if (!data.Headers.TryGetValues(ServicecontextHeader, out var header))
                throw new Exception("Missing Service Context definition in request.");

            return header.First().ToLowerInvariant();
        }
    }

    public interface IRequestContextService
    {
        string ServiceContext { get; set; }
        public Task SetRequestContext(HttpRequestData data);
    }
}
