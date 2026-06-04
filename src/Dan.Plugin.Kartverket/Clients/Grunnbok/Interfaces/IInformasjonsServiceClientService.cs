using Dan.Plugin.Kartverket.Models;
using Kartverket.Grunnbok.InformasjonsService;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces
{
    public interface IInformasjonsServiceClientService
    {
        Task<OwnerShipTransferInfo> GetOwnershipInfo(string registerenhetsid);
        Task<HeftelseInformasjonTransfer> GetPawnStuff(string registerenhetid);
        Task<HeftelseInformasjonTransfer> GetHeftelser(string registerenhetid);
        Task<RettsstiftelseInformasjonTransfer> GetRettsstiftelse(string rettstiftelseid);
    }
}
