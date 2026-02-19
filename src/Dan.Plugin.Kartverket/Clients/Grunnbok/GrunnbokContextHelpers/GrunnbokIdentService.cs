using Dan.Plugin.Kartverket.Clients.Grunnbok.GrunnbokContextHelpers;

namespace Kartverket.Grunnbok.IdentService
{
    public partial class Timestamp : ISnapshotTimestamp
    {
    }


    public partial class GrunnbokContext
        : IGrunnbokContext<Timestamp>
    {
    }
}
