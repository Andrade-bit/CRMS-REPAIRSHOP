namespace CRMS.api.Controllers
{
    /// <summary>One tenant's subscription row for the Superadmin Subscriptions
    /// page. Sourced from Companies + the tenant's one Admin ApplicationUser
    /// (whose Status is what actually blocks login), with billing-cycle
    /// bookkeeping layered on top via the Admins table.</summary>
    public record TenantSubscriptionEntry(
        int CompanyId, string CompanyCode, string CompanyName,
        string AdminUsername, string AdminName, string Status,
        string BillingCycle, DateTime? NextBillingDate, decimal? Amount, string Plan, DateTime CompanyCreatedAt);

    public record SetSubscriptionStatusRequest(string Status);
    public record SetBillingCycleRequest(string BillingCycle, DateTime? NextBillingDate);
    public record SetSubscriptionAmountRequest(decimal? Amount);

    // Plan must be "AllAccess", "Growth", or "Starter" — see
    // ModuleAccessController for what each one actually unlocks.
    public record SetSubscriptionPlanRequest(string Plan);

    public record TermsVersionEntry(int TermsId, string Version, string Content, DateTime CreatedAt, bool IsCurrent, string PlanScope);
    public record PublishTermsRequest(string Version, string Content, string PlanScope);
    public record UpdateTermsScopeRequest(string PlanScope);

    public record SuperadminAdminAccountEntry(int CompanyId, string CompanyName, string Username, string FullName, string Status);
    public record SuperadminResetPasswordRequest(string NewPassword);

    /// <summary>One tenant's slice of the platform-wide report — requires a
    /// live query against that tenant's own database, not just Master DB.</summary>
    public record TenantPlatformSummary(int CompanyId, string CompanyName, int TicketsResolved, decimal Revenue);

    public record PlatformReportResponse(
        int TicketsResolved30d, decimal Revenue30d, int ActiveStaffAccounts, double AverageRating, int RatingCount,
        List<TenantPlatformSummary> ByTenant);
}
