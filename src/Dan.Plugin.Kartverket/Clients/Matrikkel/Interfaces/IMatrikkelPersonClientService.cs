using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces
{
    public interface IMatrikkelPersonClientService
    {
        Task<long> GetOrganization(string orgno);
        Task<long> GetPerson(string nin);
    }
}
