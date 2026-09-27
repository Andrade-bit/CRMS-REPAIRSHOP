using System;
using System.Collections.Generic;
using CRMS.winforms.Api;

namespace CRMS.winforms.Controllers
{
    /// <summary>
    /// Backs SuperAdminMainForm: tracks which sidebar page is active and exposes
    /// the real platform-level operations (subscriptions, billing, Terms &amp;
    /// Agreement versioning, Admin account management, platform-wide reports) a
    /// Superadmin needs. Superadmin accounts carry no CompanyId, only a Token.
    /// </summary>
    public class SuperAdminController
    {
        private string currentPage;
        private readonly AuthApiClient _apiClient = new();

        /// <summary>The logged-in Superadmin's JWT, used to call authenticated API endpoints.</summary>
        public string Token { get; }

        public SuperAdminController(string token = "")
        {
            currentPage = "Dashboard";
            Token = token;
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

        // ===================== Subscriptions =====================

        /// <summary>Every active tenant's subscription/billing status. Returns an
        /// empty list if the request fails.</summary>
        public System.Threading.Tasks.Task<List<TenantSubscriptionEntryDto>> GetSubscriptionsAsync() =>
            _apiClient.GetSubscriptionsAsync(Token);

        /// <summary>Suspends or reactivates a tenant's Admin login — the same
        /// mechanism that blocks any other suspended account from signing in.</summary>
        public System.Threading.Tasks.Task<(bool Success, string Message)> SetSubscriptionStatusAsync(int companyId, string status) =>
            _apiClient.SetSubscriptionStatusAsync(Token, companyId, status);

        /// <summary>Updates a tenant's billing cycle bookkeeping (Monthly/Yearly, next billing date).</summary>
        public System.Threading.Tasks.Task<(bool Success, string Message)> SetBillingCycleAsync(int companyId, string billingCycle, DateTime? nextBillingDate) =>
            _apiClient.SetBillingCycleAsync(Token, companyId, billingCycle, nextBillingDate);

        /// <summary>Updates how much a tenant pays per billing cycle.</summary>
        public System.Threading.Tasks.Task<(bool Success, string Message)> SetSubscriptionAmountAsync(int companyId, decimal? amount) =>
            _apiClient.SetSubscriptionAmountAsync(Token, companyId, amount);

        /// <summary>Sets which module set a tenant's own apps unlock.</summary>
        public System.Threading.Tasks.Task<(bool Success, string Message)> SetSubscriptionPlanAsync(int companyId, string plan) =>
            _apiClient.SetSubscriptionPlanAsync(Token, companyId, plan);

        // ===================== Terms & Agreement =====================

        /// <summary>Every published Terms &amp; Agreement version, newest first.</summary>
        public System.Threading.Tasks.Task<List<TermsVersionEntryDto>> GetTermsVersionsAsync() =>
            _apiClient.GetTermsVersionsAsync(Token);

        /// <summary>Publishes a new Terms &amp; Agreement version, scoped to one
        /// plan (or "All"), and makes it current for that scope.</summary>
        public System.Threading.Tasks.Task<(bool Success, string Message)> PublishTermsVersionAsync(string version, string content, string planScope) =>
            _apiClient.PublishTermsVersionAsync(Token, version, content, planScope);

        /// <summary>The terms version currently in effect for one plan — used
        /// by the Subscriptions page to show each tenant which terms apply to them.</summary>
        public System.Threading.Tasks.Task<TermsVersionEntryDto?> GetCurrentTermsForPlanAsync(string plan) =>
            _apiClient.GetCurrentTermsForPlanAsync(Token, plan);

        /// <summary>Moves an already-published version to a different plan.</summary>
        public System.Threading.Tasks.Task<(bool Success, string Message)> UpdateTermsVersionScopeAsync(int termsId, string planScope) =>
            _apiClient.UpdateTermsVersionScopeAsync(Token, termsId, planScope);

        // ===================== User Management (Admin accounts) =====================

        /// <summary>The one Admin account per tenant, across the whole platform.
        /// Deliberately never creates new accounts — tenant provisioning is a
        /// separate, deferred feature.</summary>
        public System.Threading.Tasks.Task<List<SuperadminAdminAccountEntryDto>> GetAdminAccountsAsync() =>
            _apiClient.GetSuperadminAdminAccountsAsync(Token);

        /// <summary>Sets a new password for an Admin account.</summary>
        public System.Threading.Tasks.Task<(bool Success, string Message)> ResetAdminPasswordAsync(string username, string newPassword) =>
            _apiClient.SuperadminResetPasswordAsync(Token, username, newPassword);

        /// <summary>Suspends or reactivates an Admin account.</summary>
        public System.Threading.Tasks.Task<(bool Success, string Message)> SetAdminAccountStatusAsync(string username, string status) =>
            _apiClient.SuperadminSetAccountStatusAsync(Token, username, status);

        // ===================== Platform Reports =====================

        /// <summary>Cross-tenant aggregate figures for the last 30 days. Returns
        /// null if the request fails.</summary>
        public System.Threading.Tasks.Task<PlatformReportResponseDto?> GetPlatformReportAsync() =>
            _apiClient.GetPlatformReportAsync(Token);
    }
}
