using System;
using System.Collections.Generic;

namespace CRMS.domain.Entities
{
    public class Superadmin : ISyncTracked
    {
        public int SuperadminId { get; set; }

        // FK -> ApplicationUser (Identity handles the actual login/credentials)
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser ApplicationUser { get; set; } = null!;

        public ICollection<Admin> Admins { get; set; } = new List<Admin>();
        public ICollection<TermsAgreement> TermsAgreements { get; set; } = new List<TermsAgreement>();

        // ----- Local-first cloud sync ----- (see Staff.cs for the full explanation)
        public bool IsSynced { get; set; } = false;
        public DateTime? SyncedAt { get; set; }
        public int? CloudId { get; set; }
    }
}