using Kartverket.Matrikkel.BygningService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces
{
    public interface IMatrikkelBygningClientService
    {
        Task<List<long>> GetBygningerForMatrikkelenhet(long matrikkelEnhetId);
        Task<findAlleBygningstypeKoderResponse> GetBygningsType();
    }
}
