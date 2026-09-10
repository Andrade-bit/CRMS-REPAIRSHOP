using System;

namespace CRMS.domain.Entities
{
    public class Notification
    {
        public int NotificationId { get; set; }

        // FK -> ServiceTicket
        public int TicketId { get; set; }
        public ServiceTicket Ticket { get; set; } = null!;

        public string Channel { get; set; } = string.Empty; // e.g. SMS, Email
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}