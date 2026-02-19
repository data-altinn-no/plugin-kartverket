using System;

namespace Dan.Plugin.Kartverket.Clients.Grunnbok.GrunnbokContextHelpers
{
    public interface IGrunnbokContext<TSnapshot>
    {
        string locale { get;set; }
        string clientIdentification { get;set; }
        string clientTraceInfo { get;set; }
        string systemVersion { get;set; }
        TSnapshot snapshotVersion { get; set;  }
    }

    public interface ISnapshotTimestamp
    {
        DateTime timestamp { get; set; }
    }    
}
