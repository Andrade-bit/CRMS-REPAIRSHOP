using System;

namespace CRMS.domain.Entities
{
    // A tenant's own Terms & Conditions — separate from Superadmin's
    // platform-wide TermsAgreement (which lives in MasterDB). Lives in the
    // tenant's own database, the same way Staff/Customer/Branch do, so each
    // tenant's version history is fully isolated from every other tenant's.
    public class TenantTermsVersion
    {
        public int TermsId { get; set; }

        // FK -> Admin (who published this version) — Admin lives in MasterDB,
        // a different physical database, so this stays a plain column (no
        // HasOne), same as Staff.AdminId.
        public int AdminId { get; set; }

        public string Version { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Exactly one version is ever current at a time — publishing a new one
        // flips this off on whichever version held it before. This is the text
        // Staff sees on the Add Customer screen.
        public bool IsCurrent { get; set; }
    }
}
