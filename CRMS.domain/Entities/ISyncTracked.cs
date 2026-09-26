using System;

namespace CRMS.domain.Entities
{
    /// <summary>Implemented by every entity the local-first cloud sync worker
    /// tracks (Staff, Customer, Promotion, ServiceTicket). IsSynced is false
    /// the moment a row is created or edited locally; the sync worker flips it
    /// true once the cloud copy is confirmed written, recording CloudId (that
    /// row's primary key in the cloud database — a different number than its
    /// local id, since the two databases have independent identity sequences).</summary>
    public interface ISyncTracked
    {
        bool IsSynced { get; set; }
        DateTime? SyncedAt { get; set; }
        int? CloudId { get; set; }
    }
}
