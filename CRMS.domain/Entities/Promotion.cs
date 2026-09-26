using System;

namespace CRMS.domain.Entities
{
    /// <summary>Statuses a Promotion moves through: a Manager drafts/submits one,
    /// an Admin approves or rejects it. Nothing about actually sending it to
    /// customers is implemented yet — this is just the draft/approval workflow.</summary>
    public static class PromotionStatus
    {
        public const string Draft = "Draft";
        public const string PendingApproval = "PendingApproval";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }

    public class Promotion : ISyncTracked
    {
        public int PromotionId { get; set; }

        // FK -> Staff (who drafted it — the Staff table also holds Manager
        // accounts, since Staff/Manager share the same tenant-side identity
        // table). Lives in this same tenant DB, so a real FK is fine here.
        public int StaffId { get; set; }
        public Staff Staff { get; set; } = null!;

        // FK -> Admin (who reviewed it — lives in MasterDB, so plain column
        // only, no navigation). Null until Approved/Rejected.
        public int? ReviewedByAdminId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "General"; // e.g. Referral, Threshold Discount, Seasonal, General
        public string Message { get; set; } = string.Empty;

        // Why the Manager is proposing this discount/offer — shown to the Admin
        // reviewing it, since "Message" is the customer-facing copy and doesn't
        // explain the business justification behind it.
        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; } = PromotionStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        // ----- Local-first cloud sync ----- (see Staff.cs for the full explanation)
        public bool IsSynced { get; set; } = false;
        public DateTime? SyncedAt { get; set; }
        public int? CloudId { get; set; }
    }
}
