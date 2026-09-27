using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Controllers
{
    /// <summary>Promotion actions only Admin's screen calls today: approving or
    /// rejecting a promotion a Manager submitted.</summary>
    [ApiController]
    [Route("api/promotions")]
    [Authorize]
    public class AdminPromotionsController : ControllerBase
    {
        private readonly TenantCRMSDbContext _tenantDb;

        public AdminPromotionsController(TenantCRMSDbContext tenantDb)
        {
            _tenantDb = tenantDb;
        }

        [HttpPut("{id}/approve")]
        public Task<IActionResult> Approve(int id) => SetStatus(id, PromotionStatus.Approved);

        [HttpPut("{id}/reject")]
        public Task<IActionResult> Reject(int id) => SetStatus(id, PromotionStatus.Rejected);

        /// <summary>Hides a promotion from Admin's review list. Never a real
        /// delete — the record (and its approval/rejection history) is kept.</summary>
        [HttpPut("{id}/archive")]
        public async Task<IActionResult> Archive(int id)
        {
            var promotion = await _tenantDb.Promotions.FirstOrDefaultAsync(p => p.PromotionId == id);
            if (promotion is null)
                return NotFound();

            promotion.ArchivedAt = DateTime.UtcNow;
            await _tenantDb.SaveChangesAsync();

            return Ok();
        }

        private async Task<IActionResult> SetStatus(int id, string status)
        {
            var promotion = await _tenantDb.Promotions.FirstOrDefaultAsync(p => p.PromotionId == id);
            if (promotion is null)
                return NotFound();

            promotion.Status = status;
            promotion.ReviewedAt = DateTime.UtcNow;
            // ReviewedByAdminId is left null for now — resolving "which Admin" would
            // need a MasterCRMSDbContext lookup, and nothing displays it yet.
            await _tenantDb.SaveChangesAsync();

            return Ok();
        }
    }
}
