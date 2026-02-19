using Dan.Plugin.Kartverket.Clients.Matrikkel.MatrikkelContextHelpers;

namespace Kartverket.Matrikkel.PersonService
{
    public partial class Timestamp : ISnapshotTimestamp
    {
    }

    public partial class KoordinatsystemKodeId : IKoordinatsystemKodeId
    {
    }

    public partial class MatrikkelContext
        : IMatrikkelContext<Timestamp, KoordinatsystemKodeId>
    {
    }


}
