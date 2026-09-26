using CRMS.infrastructure.Data;
using CRMS.infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// Superadmin's Platform Reports page — the one place that genuinely
    /// crosses tenant boundaries: it opens a short-lived TenantCRMSDbContext
    /// against EACH active tenant's own database in turn (using the same
    /// connection-string resolution every per-tenant request already relies
    /// on) and aggregates the results. A tenant whose database can't be
    /// reached (e.g. missing TenantCredentials config) is skipped rather than
    /// failing the whole report — one bad tenant shouldn't blank the page.
    /// </summary>
    [ApiController]
    [Route("api/superadmin/platform-report")]
    [Authorize(Roles = "Superadmin")]
    public class SuperadminReportsController : ControllerBase
    {
        private static readonly string[] RevenueStatuses = { "Repaired", "Cancelled", "Unrepairable" };

        private readonly MasterCRMSDbContext _masterDb;
        private readonly ITenantConnectionService _tenantConnectionService;

        public SuperadminReportsController(MasterCRMSDbContext masterDb, ITenantConnectionService tenantConnectionService)
        {
            _masterDb = masterDb;
            _tenantConnectionService = tenantConnectionService;
        }

        [HttpGet]
        public async Task<ActionResult<PlatformReportResponse>> GetPlatformReport()
        {
            var companies = await _masterDb.Companies.Where(c => c.IsActive).ToListAsync();
            var since = DateTime.UtcNow.AddDays(-30);

            var byTenant = new List<TenantPlatformSummary>();
            int totalActiveStaff = 0;
            decimal totalRating = 0;
            int totalRatingCount = 0;

            foreach (var company in companies)
            {
                string connectionString;
                try
                {
                    connectionString = await _tenantConnectionService.GetTenantConnectionStringAsync(company.CompanyId);
                }
                catch (InvalidOperationException)
                {
                    continue; // no TenantCredentials configured for this tenant — skip, don't fail the whole report
                }

                var options = new DbContextOptionsBuilder<TenantCRMSDbContext>().UseSqlServer(connectionString).Options;
                await using var tenantDb = new TenantCRMSDbContext(options);

                int ticketsResolved;
                decimal revenue;
                int activeStaff;
                List<int> ratings;
                try
                {
                    var resolved = await tenantDb.ServiceTickets
                        .Where(t => RevenueStatuses.Contains(t.Status) && t.CompletedAt != null && t.CompletedAt >= since)
                        .Select(t => t.EstimatedCost)
                        .ToListAsync();
                    ticketsResolved = resolved.Count;
                    revenue = resolved.Sum();

                    activeStaff = await tenantDb.Staff.CountAsync(s => s.Status == "Active");
                    ratings = await tenantDb.Feedbacks.Select(f => f.Rating).ToListAsync();
                }
                catch (Exception)
                {
                    continue; // tenant DB unreachable/misconfigured — skip, don't fail the whole report
                }

                byTenant.Add(new TenantPlatformSummary(company.CompanyId, company.CompanyName, ticketsResolved, revenue));
                totalActiveStaff += activeStaff;
                totalRating += ratings.Sum();
                totalRatingCount += ratings.Count;
            }

            double averageRating = totalRatingCount > 0 ? (double)totalRating / totalRatingCount : 0;

            var response = new PlatformReportResponse(
                byTenant.Sum(t => t.TicketsResolved),
                byTenant.Sum(t => t.Revenue),
                totalActiveStaff,
                averageRating,
                totalRatingCount,
                byTenant.OrderByDescending(t => t.Revenue).ToList());

            return Ok(response);
        }
    }
}
