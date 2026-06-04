using Dan.Plugin.Kartverket.Models;
using Kartverket.Grunnbok.StoreService;
using System.Collections.Generic;
using System.Threading.Tasks;
using KommuneDAN = Dan.Plugin.Kartverket.Models.Kommune;
using Matrikkelenhet = Kartverket.Grunnbok.StoreService.Matrikkelenhet;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok.Interfaces
{
    public interface IStoreServiceClientService
    {
        Task<KommuneDAN> GetKommune(string kommuneIdent);
        Task<Registerenhetsrettsandel> GetRettighetsandeler(string id);
        Task<Registerenhetsrett> GetRegisterenhetsrett(string id);
        Task<Rettsstiftelse> GetRettsstiftelse(string id);
        Task<Dokument> GetDokument(string id);
        Task<List<PawnDocument>> GetPawnOwnerNames(List<PawnDocument> input);
        Task<Matrikkelenhet> GetRegisterenhet(string registerenhetid);
        Task<Matrikkelenhet> GetMatrikkelEnhetFromRegisterRettighetsandel(string registerrettighetsandelid);
        Task<Person> GetPerson(string personId);
    }
}
