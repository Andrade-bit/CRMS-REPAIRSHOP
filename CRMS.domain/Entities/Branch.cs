using System;

namespace CRMS.domain.Entities
{
    // Lives in the tenant's own database (not Master) since a branch is that
    // tenant's own operational concept, the same way Staff/Customers are.
    // A tenant can have many rows here — Staff/ServiceTicket reference one via
    // BranchId, nullable so a Staff/ticket can also belong to the tenant
    // directly with no specific branch.
    public class Branch : ISyncTracked
    {
        public int BranchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Never a real delete — Staff/ServiceTicket rows already pointing at
        // this branch keep that BranchId. Archiving just hides it from the
        // branch list and from being assigned to new Staff/tickets.
        public DateTime? ArchivedAt { get; set; }

        // ----- Local-first cloud sync ----- (see Staff.cs for the full explanation)
        public bool IsSynced { get; set; } = false;
        public DateTime? SyncedAt { get; set; }
        public int? CloudId { get; set; }
    }
}
