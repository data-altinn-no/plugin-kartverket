using Kartverket.Grunnbok.RegisterenhetsrettService;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces
{
    public interface IRegisterenhetsrettClientService
    {
        Task<RegisterenhetIdTilRegisterenhetsrettIdsMap> GetRetterForEnheter(string registerenhetsid);
    }
}
