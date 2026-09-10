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
    }
}