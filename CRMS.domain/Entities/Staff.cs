using System.Collections.Generic;

namespace CRMS.domain.Entities
{
    public class Staff
    {
        public int StaffId { get; set; }

        // FK -> ApplicationUser (lives in MasterDB — plain column only, no navigation)
        public string ApplicationUserId { get; set; } = string.Empty;

        // FK -> Admin (lives in MasterDB — plain column only, no navigation)
        public int AdminId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // e.g. Active, Suspended

        public ICollection<ServiceTicket> ServiceTickets { get; set; } = new List<ServiceTicket>();
    }
}