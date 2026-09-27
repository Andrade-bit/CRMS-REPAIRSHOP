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
    /// Listing promotions is shared — Manager's own page and Admin's review page
    /// both call this same endpoint, just with a different query.
    /// </summary>
    [ApiController]
    [Route("api/promotions")]
    [Authorize]
    public class PromotionsController : ControllerBase
    {
        private readonly TenantCRMSDbContext _tenantDb;

        public PromotionsController(TenantCRMSDbContext tenantDb)
        {
            _tenantDb = tenantDb;
        }

        /// <summary>mine=true returns only the caller's own submissions (Manager's
        /// own list); omitted/false returns every promotion in the tenant (Admin's
        /// review list).</summary>
        [HttpGet]
        public async Task<ActionResult<List<PromotionResponse>>> GetPromotions([FromQuery] bool mine = false)
        {
            var query = _tenantDb.Promotions.Include(p => p.Staff).Where(p => p.ArchivedAt == null).AsQueryable();

            if (mine)
            {
                var staff = await FindCurrentStaffAsync();
                if (staff is null) return Ok(new List<PromotionResponse>());
                query = query.Where(p => p.StaffId == staff.StaffId);
            }

            var promotions = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            var result = promotions.Select(p => new PromotionResponse(
                p.PromotionId, p.Title, p.Type, p.Message, p.Reason, p.Status,
                p.Staff.Name, p.CreatedAt, p.ReviewedAt)).ToList();

            return Ok(result);
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
