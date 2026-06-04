using Kartverket.Matrikkel.BruksenhetService;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces
{
    public interface IMatrikkelBruksenhetService
    {
        Task<BruksenhetId[]> GetBruksenheter(long matrikkelEnhetId);
        Task<string> GetAddressForBruksenhet(long bruksenhetId);
    }
}
