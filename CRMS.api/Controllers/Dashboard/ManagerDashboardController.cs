using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// Backs Manager's Dashboard page — personal, not shop-wide. Manager doesn't
    /// oversee other staff or the shop's revenue (that's Admin's dashboard); this
    /// is scoped to the logged-in Manager's own tickets and their own promotions.
    /// </summary>
    [ApiController]
    [Route("api/manager/dashboard")]
    [Authorize]
    public class ManagerDashboardController : ControllerBase
    {
        private static readonly string[] ActiveStatuses = { "Diagnose", "AwaitingApproval", "Repairing" };

        private readonly TenantCRMSDbContext _tenantDb;

        public ManagerDashboardController(TenantCRMSDbContext tenantDb)
        {
            _tenantDb = tenantDb;
        }

        [HttpGet]
        public async Task<ActionResult<ManagerDashboardResponse>> GetDashboard()
        {
            var staff = await FindCurrentStaffAsync();
            if (staff is null)
                return BadRequest("No Staff record found in this tenant for the logged-in account.");

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var myTickets = _tenantDb.ServiceTickets
                .Where(t => t.ArchivedAt == null && t.StaffId == staff.StaffId);

            int myActiveTickets = await myTickets.CountAsync(t => ActiveStatuses.Contains(t.Status));
            int awaitingCustomerApproval = await myTickets.CountAsync(t => t.Status == "AwaitingApproval");

            var myTicketPipeline = await myTickets
                .Where(t => ActiveStatuses.Contains(t.Status))
                .GroupBy(t => t.Status)
                .Select(g => new PipelineEntry(g.Key, g.Count()))
                .ToListAsync();

            var myPromotions = _tenantDb.Promotions.Where(p => p.StaffId == staff.StaffId);

            int promotionsAwaitingAdmin = await myPromotions.CountAsync(p => p.Status == PromotionStatus.PendingApproval);
            int promotionsApprovedMtd = await myPromotions.CountAsync(p =>
                p.Status == PromotionStatus.Approved && p.ReviewedAt != null && p.ReviewedAt >= monthStart);

            var myPromotionsBreakdown = await myPromotions
                .GroupBy(p => p.Status)
                .Select(g => new PromotionStatusEntry(g.Key, g.Count()))
                .ToListAsync();

            var recentPromotions = await myPromotions
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PromotionSummaryEntry(p.PromotionId, p.Title, p.Type, p.Status, p.CreatedAt))
                .ToListAsync();

            return Ok(new ManagerDashboardResponse(
                myActiveTickets, awaitingCustomerApproval, promotionsAwaitingAdmin, promotionsApprovedMtd,
                myTicketPipeline, myPromotionsBreakdown, recentPromotions));
        }

        private async Task<Staff?> FindCurrentStaffAsync()
        {
            string? userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return null;

            return await _tenantDb.Staff.FirstOrDefaultAsync(s => s.ApplicationUserId == userId);
        }
    }
}
