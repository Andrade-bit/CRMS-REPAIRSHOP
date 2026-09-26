using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Controllers
{
    public record ModuleAccessResponse(
        bool Promotions, bool Reports, bool Branches,
        bool PdfExport, bool EmailNotifications, bool OfflineSync);

    /// <summary>
    /// Tells a tenant's own Admin/Manager/Staff app which sidebar modules to
    /// show, based on that tenant's SubscriptionPlan. Ticket workflow,
    /// customer records, and Staff/Manager accounts are never gated — every
    /// plan includes them. This is UI-hiding guidance only, not an access
    /// control boundary: the underlying endpoints for a hidden module still
    /// work if called directly.
    /// </summary>
    [ApiController]
    [Route("api/modules")]
    [Authorize]
    public class ModuleAccessController : ControllerBase
    {
        private readonly MasterCRMSDbContext _masterDb;

        public ModuleAccessController(MasterCRMSDbContext masterDb)
        {
            _masterDb = masterDb;
        }

        [HttpGet]
        public async Task<ActionResult<ModuleAccessResponse>> GetModuleAccess()
        {
            string? companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (!int.TryParse(companyIdClaim, out var companyId))
                return Ok(Resolve(null));

            var admin = await _masterDb.Admins
                .Include(a => a.ApplicationUser)
                .FirstOrDefaultAsync(a => a.ApplicationUser.CompanyId == companyId);

            return Ok(Resolve(admin?.SubscriptionPlan));
        }

        // Unset/unrecognized plan resolves the same as "AllAccess" — a
        // tenant never loses access because of a missing or bad plan value.
        private static ModuleAccessResponse Resolve(string? plan) => plan switch
        {
            "Growth" => new ModuleAccessResponse(
                Promotions: true, Reports: true, Branches: false,
                PdfExport: true, EmailNotifications: true, OfflineSync: true),

            "Starter" => new ModuleAccessResponse(
                Promotions: false, Reports: false, Branches: false,
                PdfExport: true, EmailNotifications: true, OfflineSync: true),

            _ => new ModuleAccessResponse(
                Promotions: true, Reports: true, Branches: true,
                PdfExport: true, EmailNotifications: true, OfflineSync: true),
        };
    }
}
