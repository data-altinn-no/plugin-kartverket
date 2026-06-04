using Kartverket.Grunnbok.RegisterenhetsrettsandelService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces
{
    public interface IRegisterenhetsRettsandelsServiceClientService
    {
        Task<List<string>> GetAndelerForRettighetshaver(string personident);
        Task<findAndelerIRetterResponse> GetAndelerIRetter(string registerenhetsid);
    }
}
