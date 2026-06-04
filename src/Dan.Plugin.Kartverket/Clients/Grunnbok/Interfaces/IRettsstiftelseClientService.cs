using Dan.Plugin.Kartverket.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces
{
    public interface IRettsstiftelseClientService
    {
        Task<List<PawnDocument>> GetHeftelser(string registerenhetid);
    }
}
