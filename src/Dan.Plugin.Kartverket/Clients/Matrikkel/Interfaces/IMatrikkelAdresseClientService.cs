using Kartverket.Matrikkel.AdresseService;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces
{
    public interface IMatrikkelAdresseClientService
    {
        Task<AdresseId[]> GetAdresserForMatrikkelenhet(long matrikkelEnhetId);
        Task<AdresseId[]> FindAdresser(string adresseNavn, string kommuneNo);
    }
}
