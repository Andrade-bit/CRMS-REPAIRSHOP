using System.Collections.Generic;
using System.Linq;

namespace CRMS.winforms.Controllers
{
    /// <summary>
    /// Backs SuperAdminMainForm: tracks which sidebar page is active and exposes
    /// the platform-level operations (onboarding tenants, subscription status,
    /// Terms &amp; Conditions versions, platform-wide analytics) a Superadmin needs.
    /// </summary>
    public class SuperAdminController
    {
        private string currentPage;

        public SuperAdminController()
        {
            currentPage = "Tenants";
        }

        // Changes the current page
        public string NavigateTo(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName))
            {
                return currentPage;
            }

            currentPage = pageName;

            return currentPage;
        }

        // Gets the current page
        public string GetCurrentPage()
        {
            return currentPage;
        }

        // ===================== Tenants / subscriptions =====================

        public IReadOnlyList<TenantRecord> GetTenants() => AppDataStore.Tenants;

        public TenantRecord CreateTenant(string shopName, string adminFullName, string adminUsername)
        {
            return AppDataStore.AddTenant(shopName, adminFullName, adminUsername);
        }

        public void UpdateTenantStatus(int tenantId, string newStatus)
        {
            AppDataStore.UpdateTenantSubscriptionStatus(tenantId, newStatus);
        }

        // ===================== Terms & Conditions =====================

        public IReadOnlyList<TermsVersionRecord> GetTermsVersions() => AppDataStore.TermsVersions;

        public TermsVersionRecord PublishTermsVersion(string version, string content)
        {
            return AppDataStore.AddTermsVersion(version, content);
        }

        public void SetCurrentTermsVersion(int termsId)
        {
            AppDataStore.SetCurrentTermsVersion(termsId);
        }

        // ===================== Analytics (platform-wide) =====================

        public int TotalTenants => AppDataStore.Tenants.Count;

        public int ActiveTenants => AppDataStore.Tenants.Count(t => t.SubscriptionStatus == TenantSubscriptionStatus.Active);

        public int SuspendedTenants => AppDataStore.Tenants.Count(t => t.SubscriptionStatus == TenantSubscriptionStatus.Suspended);

        public int TotalTicketsProcessed => AppDataStore.PendingTickets.Count + AppDataStore.TicketHistory.Count;

        public decimal TotalRevenue => AppDataStore.TicketHistory.Sum(t => t.EstimatedCost);
    }
}
