using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces
{
    public interface IIdentServiceClientService
    {
        Task<string> GetPersonIdentity(string personId);
    }
}
