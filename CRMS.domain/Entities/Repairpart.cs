namespace CRMS.domain.Entities
{
    public class RepairPart
    {
        public int PartId { get; set; }

        // FK -> ServiceTicket
        public int TicketId { get; set; }
        public ServiceTicket Ticket { get; set; } = null!;

        public string PartName { get; set; } = string.Empty;
        public decimal PartCost { get; set; }
    }
}