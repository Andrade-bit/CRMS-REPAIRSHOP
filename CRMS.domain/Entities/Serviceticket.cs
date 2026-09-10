using System;
using System.Collections.Generic;

namespace CRMS.domain.Entities
{
    public class ServiceTicket
    {
        public int TicketId { get; set; }

        // FK -> Customer (who submitted the device)
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        // FK -> Staff (who is handling the repair)
        public int? StaffId { get; set; }
        public Staff? Staff { get; set; }

        public string DeviceName { get; set; } = string.Empty;
        public string Concern { get; set; } = string.Empty;
        public string Status { get; set; } = "Received"; // Received, Diagnosing, Waiting for Parts, In Repair, Ready for Pickup
        public decimal EstimatedCost { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool TermsAccepted { get; set; }

        public ICollection<RepairPart> RepairParts { get; set; } = new List<RepairPart>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}