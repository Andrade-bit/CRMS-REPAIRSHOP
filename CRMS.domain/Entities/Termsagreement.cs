using System;

namespace CRMS.domain.Entities
{
    public class TermsAgreement : ISyncTracked
    {
        public int TermsId { get; set; }

        // FK -> Superadmin (who authored this version)
        public int SuperadminId { get; set; }
        public Superadmin Superadmin { get; set; } = null!;

        public string Version { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // "All" (the default/fallback), or one specific plan this version's
        // terms apply to: "Starter", "Growth", "AllAccess" — same plan names
        // Admin.SubscriptionPlan uses.
        public string PlanScope { get; set; } = "All";

        // Exactly one version is ever current PER PlanScope — publishing a
        // new "Growth" version only flips this off on the previous current
        // "Growth" version, leaving Starter/AllAccess/All untouched.
        public bool IsCurrent { get; set; }

        // ----- Local-first cloud sync ----- (see Staff.cs for the full explanation)
        public bool IsSynced { get; set; } = false;
        public DateTime? SyncedAt { get; set; }
        public int? CloudId { get; set; }
    }
}