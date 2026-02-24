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
        public string ServiceContext { get; set; }

        public Task SetRequestContext(string serviceContext)
        {
            ServiceContext = serviceContext;
            return Task.CompletedTask;
        }

    }

    public interface IRequestContextService
    {
        string ServiceContext { get; set; }
        public Task SetRequestContext(string serviceContext);
    }
}
