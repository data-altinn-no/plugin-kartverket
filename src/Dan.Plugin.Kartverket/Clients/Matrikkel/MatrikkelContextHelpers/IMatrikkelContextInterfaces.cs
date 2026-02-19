using System;

namespace Dan.Plugin.Kartverket.Clients.Matrikkel.MatrikkelContextHelpers
{
    public interface IMatrikkelContext<TSnapshot, TKoordinatsystem>
    {
        string locale { get; set; }
        bool brukOriginaleKoordinater { get; set; }
        TKoordinatsystem koordinatsystemKodeId { get; set; }
        string klientIdentifikasjon { get; set; }
        TSnapshot snapshotVersion { get; set; }
        string systemVersion { get; set; }
    }

    public interface ISnapshotTimestamp
    {
        DateTime timestamp { get; set; }
    }

    public interface IKoordinatsystemKodeId
    {
        long value { get; set; }
    }
}
