using Dan.Plugin.Kartverket.Models;
using Kartverket.Matrikkel.MatrikkelenhetService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel.Interfaces
{
    public interface IMatrikkelenhetClientService
    {
        Task<MatrikkelenhetId> GetMatrikkelenhet(int gnr, int bnr, int fnr, int seksjonsnummer, string kommuneIdent);
        Task<List<MatrikkelenhetId>> GetMatrikkelenheterForPerson(long ident);
        Task<MatrikkelenhetMedTeigerTransfer> GetMatrikkelEnhetMedTeiger(int gnr, int bnr, int fnr, int seksjonsnummer, string kommuneIdent);
        Task<MatrikkelEnhetMedteig> GetMatrikkelEnhetTeig(int gnr, int bnr, int fnr, int seksjonsnummer, string kommuneIdent);
        Task<MatrikkelenhetId> GetMatrikkelenhetByMatrikkelnummer(int gnr, int bnr, string kommuneIdent);
    }
}
