using System.Collections.Generic;

namespace CRMS.domain.Entities
{
    public class Superadmin
    {
        public int SuperadminId { get; set; }

        // FK -> ApplicationUser (Identity handles the actual login/credentials)
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser ApplicationUser { get; set; } = null!;

        public ICollection<Admin> Admins { get; set; } = new List<Admin>();
        public ICollection<TermsAgreement> TermsAgreements { get; set; } = new List<TermsAgreement>();
    }
}