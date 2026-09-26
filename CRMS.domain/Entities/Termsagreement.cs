using System;

namespace CRMS.domain.Entities
{
    public class TermsAgreement
    {
        public int TermsId { get; set; }

        // FK -> Superadmin (who authored this version)
        public int SuperadminId { get; set; }
        public Superadmin Superadmin { get; set; } = null!;

        public string Version { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Exactly one version is ever current at a time — publishing a new one
        // flips this off on whichever version held it before.
        public bool IsCurrent { get; set; }
    }
}