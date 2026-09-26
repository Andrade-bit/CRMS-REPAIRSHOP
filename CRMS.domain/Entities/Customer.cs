using System.Collections.Generic;

namespace CRMS.domain.Entities
{
    public class Customer : ISyncTracked
    {
        public int CustomerId { get; set; }

        // FK -> Admin (lives in MasterDB — plain column only, no navigation)
        public int AdminId { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // ----- Local-first cloud sync ----- (see Staff.cs for the full explanation)
        public bool IsSynced { get; set; } = false;
        public System.DateTime? SyncedAt { get; set; }
        public int? CloudId { get; set; }

        public ICollection<ServiceTicket> ServiceTickets { get; set; } = new List<ServiceTicket>();
    }
}