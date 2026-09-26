using System;
using System.Collections.Generic;

namespace CRMS.domain.Entities
{
    public class ServiceTicket : ISyncTracked
    {
        public int TicketId { get; set; }

        // FK -> Customer (who submitted the device)
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        // FK -> Staff (who is handling the repair)
        public int? StaffId { get; set; }
        public Staff? Staff { get; set; }

        // Which branch this ticket belongs to — stamped automatically from the
        // handling Staff's own BranchId at creation, never chosen separately.
        // Null means it belongs to the tenant directly (no specific branch).
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public string DeviceName { get; set; } = string.Empty;
        public string Concern { get; set; } = string.Empty;
        public string Status { get; set; } = "Received"; // Received, Diagnosing, Waiting for Parts, In Repair, Ready for Pickup
        public decimal EstimatedCost { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";

        // How much the customer has actually paid so far — PaymentStatus alone
        // ("Partial") doesn't say how much is still owed; this does.
        public decimal AmountPaid { get; set; } = 0m;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public bool TermsAccepted { get; set; }

        // ----- Customer approval before repair begins -----
        public string? DiagnosisFindings { get; set; }

        // NotRequired (default, pre-existing tickets and new ones before diagnosis),
        // Pending (findings recorded, awaiting the customer's decision), Approved, Rejected.
        public string ApprovalStatus { get; set; } = "NotRequired";
        public decimal? ApprovedAmount { get; set; }
        public DateTime? ApprovalDecisionAt { get; set; }

        // How the customer's decision was communicated — "In-person", "Phone", "SMS", "Email".
        public string? ApprovalMethod { get; set; }

        // Which approved promotion, if any, the customer used on this ticket. The
        // discount itself isn't computed automatically — Promotion is just written
        // offer copy today — Staff reads it and adjusts EstimatedCost themselves;
        // this just remembers which promotion applied, for records/reporting.
        public int? PromotionId { get; set; }
        public Promotion? Promotion { get; set; }

        // The price before the promotion's discount was applied, only ever set
        // when Staff records one (null on every ticket with no promotion). Kept
        // separate from EstimatedCost (which stays the real, final agreed price
        // that revenue/balance/reports already rely on everywhere) so a
        // discount's size is a real recorded number instead of something you'd
        // have to guess by comparing to a similar undiscounted ticket.
        public decimal? OriginalPrice { get; set; }

        // ----- Local-first cloud sync ----- (see Staff.cs for the full explanation)
        public bool IsSynced { get; set; } = false;
        public DateTime? SyncedAt { get; set; }
        public int? CloudId { get; set; }

        public ICollection<RepairPart> RepairParts { get; set; } = new List<RepairPart>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}