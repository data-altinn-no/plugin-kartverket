using Kartverket.Matrikkel.StoreService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces
{
    public interface IMatrikkelStoreClientService
    {
        Task<Matrikkelenhet> GetMatrikkelenhet(long ident);
        Task<Bygning> GetBygning(long bygningId);
        Task<Seksjon> GetMatrikkelenhetSeksjon(long ident);
        Task<Adresse> GetAdresse(long ident);
        Task<Veg> GetVeg(long ident);
        Task<Krets> GetKrets(long ident);
        Task<Bruksenhet> GetBruksenhet(long ident);
        Task<Kommune> GetKommune(long ident);
        Task<BruksenhetstypeKode> GetBruksenhetstype(long ident);

        // Bulk variants: one StoreService round trip for the whole id set (missing ids are omitted)
        Task<List<Bruksenhet>> GetBruksenheter(IEnumerable<long> idents);
        Task<List<Adresse>> GetAdresser(IEnumerable<long> idents);
        Task<List<Bygning>> GetBygninger(IEnumerable<long> idents);
        Task<List<Veg>> GetVeger(IEnumerable<long> idents);
        Task<List<Krets>> GetKretser(IEnumerable<long> idents);
        Task<List<BruksenhetstypeKode>> GetBruksenhetstyper(IEnumerable<long> idents);
    }
}
