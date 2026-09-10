namespace CRMS.domain.Entities
{
    public class Feedback
    {
        public int FeedbackId { get; set; }

        // FK -> ServiceTicket
        public int TicketId { get; set; }
        public ServiceTicket Ticket { get; set; } = null!;

        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }
}