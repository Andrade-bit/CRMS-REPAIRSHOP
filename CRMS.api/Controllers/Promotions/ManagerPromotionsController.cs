using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CRMS.api.Controllers
{
    /// <summary>Promotion actions only Manager's screen calls today: drafting and
    /// submitting a new promotion for Admin to review.</summary>
    [ApiController]
    [Route("api/promotions")]
    [Authorize]
    public class ManagerPromotionsController : ControllerBase
    {
        private readonly TenantCRMSDbContext _tenantDb;

        public ManagerPromotionsController(TenantCRMSDbContext tenantDb)
        {
            _tenantDb = tenantDb;
        }

        [HttpPost]
        public async Task<ActionResult<PromotionResponse>> CreatePromotion(CreatePromotionRequest request)
        {
            // A reason is only actually required once it's headed to Admin for a
            // real decision — a Draft is still a work-in-progress the Manager
            // hasn't finished filling out yet.
            if (request.SubmitForApproval && string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest("Please explain why this promotion should be approved.");

            var staff = await FindCurrentStaffAsync();
            if (staff is null)
                return BadRequest("No Staff record found in this tenant for the logged-in account.");

            var promotion = new Promotion
            {
                StaffId = staff.StaffId,
                Title = request.Title,
                Type = string.IsNullOrWhiteSpace(request.Type) ? "General" : request.Type,
                Message = request.Message,
                Reason = request.Reason,
                Status = request.SubmitForApproval ? PromotionStatus.PendingApproval : PromotionStatus.Draft
            };
            _tenantDb.Promotions.Add(promotion);
            await _tenantDb.SaveChangesAsync();

            return Ok(new PromotionResponse(
                promotion.PromotionId, promotion.Title, promotion.Type, promotion.Message, promotion.Reason,
                promotion.Status, staff.Name, promotion.CreatedAt, promotion.ReviewedAt));
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
