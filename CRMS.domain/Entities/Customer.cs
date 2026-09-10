using System.Collections.Generic;

namespace CRMS.domain.Entities
{
    public class Customer
    {
        public int CustomerId { get; set; }

        // FK -> Admin (lives in MasterDB — plain column only, no navigation)
        public int AdminId { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public ICollection<ServiceTicket> ServiceTickets { get; set; } = new List<ServiceTicket>();
    }
}