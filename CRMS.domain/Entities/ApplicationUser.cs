using Microsoft.AspNetCore.Identity;

namespace CRMS.domain.Entities
{
    /// <summary>
    /// Extends Identity's built-in user with fields specific to CRMS.
    /// Every login (Superadmin, Admin, Staff) is an ApplicationUser;
    /// their role (Identity Role) determines what they can do.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        // Which repair-shop client this user belongs to.
        // Null for a Superadmin (they aren't tied to one company).
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }

        public string Status { get; set; } = "Active"; // Active, Suspended, etc.
    }
}