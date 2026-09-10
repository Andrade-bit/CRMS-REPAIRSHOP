using System;

namespace CRMS.domain.Entities
{
    public class Payment
    {
        public int PaymentId { get; set; }

        // FK -> ServiceTicket
        public int TicketId { get; set; }
        public ServiceTicket Ticket { get; set; } = null!;

        public decimal Amount { get; set; }
        public string PaymentType { get; set; } = string.Empty; // e.g. Cash, GCash, Card
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    }
}