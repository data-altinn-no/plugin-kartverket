using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces
{
    public interface IMatrikkelKommuneClientService
    {
        Task<string> GetKommune(string kommunenummer);
    }
}
