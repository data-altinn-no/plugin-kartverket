using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces
{
    public interface IOverfoeringServiceClientService
    {
        Task<string> GetOverfoeringerTil(List<string> ids);
    }
}
