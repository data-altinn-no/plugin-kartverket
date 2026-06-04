using Kartverket.Matrikkel.StoreService;
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
    }
}
