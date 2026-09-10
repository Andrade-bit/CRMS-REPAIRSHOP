using System.Collections.Generic;

namespace CRMS.domain.Entities
{
    public class Admin
    {
        public int AdminId { get; set; }

        // FK -> ApplicationUser (Identity handles the actual login/credentials)
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser ApplicationUser { get; set; } = null!;

        // FK -> Superadmin (who oversees this admin)
        public int? SuperadminId { get; set; }
        public Superadmin? Superadmin { get; set; }

        public string Name { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string SubscriptionStatus { get; set; } = "Active";
    }
}