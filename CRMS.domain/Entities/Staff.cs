using System.Collections.Generic;

namespace CRMS.domain.Entities
{
    public class Staff : ISyncTracked
    {
        public int StaffId { get; set; }

        // FK -> ApplicationUser (lives in MasterDB — plain column only, no navigation)
        public string ApplicationUserId { get; set; } = string.Empty;

        // FK -> Admin (lives in MasterDB — plain column only, no navigation)
        public int AdminId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // e.g. Active, Suspended

        // FK -> Branch (same tenant DB — a real FK, unlike AdminId above).
        // Null means this Staff/Manager belongs to the tenant directly, not
        // any specific branch.
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        // ----- Local-first cloud sync -----
        // False the moment this row is created/edited locally; the background
        // sync worker flips it true once the cloud copy is confirmed written.
        // CloudId is this row's primary key in the CLOUD database once synced
        // (null until then) — local and cloud IDs are independent sequences,
        // so anything that references this row when pushing to the cloud must
        // use CloudId, never StaffId.
        public bool IsSynced { get; set; } = false;
        public System.DateTime? SyncedAt { get; set; }
        public int? CloudId { get; set; }

        public ICollection<ServiceTicket> ServiceTickets { get; set; } = new List<ServiceTicket>();
    }
}