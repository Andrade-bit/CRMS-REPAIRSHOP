using System;
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

        // Kept in sync with ApplicationUser.Status by Superadmin's Suspend/
        // Reactivate action — ApplicationUser.Status is what actually blocks
        // login (same mechanism as Staff/Manager suspension), this is just the
        // display-facing mirror of it for the Subscriptions page.
        public string SubscriptionStatus { get; set; } = "Active";

        // "Monthly" or "Yearly" — bookkeeping only, no real payment processor
        // behind it; Superadmin sets this by hand to reflect what was actually
        // agreed with the tenant.
        public string BillingCycle { get; set; } = "Monthly";
        public DateTime? NextBillingDate { get; set; }

        // How much this tenant pays per billing cycle — bookkeeping only, same
        // as BillingCycle; Superadmin sets it by hand to reflect what was
        // actually agreed with the tenant.
        public decimal? SubscriptionAmount { get; set; }

        // "AllAccess", "Growth", or "Starter" — which modules this tenant's
        // own Admin/Manager/Staff apps show in their sidebars. Resolved by
        // CRMS.api's module-access endpoint; defaults every tenant to full
        // access until a Superadmin explicitly picks a different plan.
        public string SubscriptionPlan { get; set; } = "AllAccess";
    }
}