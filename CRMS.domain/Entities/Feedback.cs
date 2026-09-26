using System;

namespace CRMS.domain.Entities
{
    public class Feedback
    {
        public int FeedbackId { get; set; }

        // FK -> ServiceTicket
        public int TicketId { get; set; }
        public ServiceTicket Ticket { get; set; } = null!;

        // 1-5 stars, always required — the message is the only optional part.
        public int Rating { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}