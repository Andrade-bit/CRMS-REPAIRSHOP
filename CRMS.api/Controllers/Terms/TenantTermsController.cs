using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// A tenant's own Terms &amp; Conditions — version history and publishing, for
    /// Admin, plus a lightweight "current version" lookup any authenticated
    /// tenant role (Staff/Manager included) can call, since that's what the
    /// Add Customer screen shows the customer before a ticket is created.
    /// Separate from Superadmin's platform-wide Terms &amp; Agreement, which lives
    /// in MasterDB — this one is scoped to whichever tenant database the
    /// caller's own token resolves to.
    /// </summary>
    [ApiController]
    [Route("api/terms")]
    [Authorize]
    public class TenantTermsController : ControllerBase
    {
        private readonly TenantCRMSDbContext _tenantDb;
        private readonly MasterCRMSDbContext _masterDb;

        public TenantTermsController(TenantCRMSDbContext tenantDb, MasterCRMSDbContext masterDb)
        {
            _tenantDb = tenantDb;
            _masterDb = masterDb;
        }

        /// <summary>Every version published for this tenant, newest first — Admin only.</summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<TenantTermsVersionEntry>>> GetVersions()
        {
            var versions = await _tenantDb.TermsVersions
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TenantTermsVersionEntry(t.TermsId, t.Version, t.Content, t.CreatedAt, t.IsCurrent))
                .ToListAsync();

            return Ok(versions);
        }

        /// <summary>The current version's content, or nulls if this tenant hasn't
        /// published one yet — callable by any authenticated tenant role, since
        /// Staff/Manager need this on the Add Customer screen.</summary>
        [HttpGet("current")]
        public async Task<ActionResult<CurrentTermsResponse>> GetCurrent()
        {
            var current = await _tenantDb.TermsVersions.FirstOrDefaultAsync(t => t.IsCurrent);
            return Ok(new CurrentTermsResponse(current?.Version, current?.Content));
        }

        /// <summary>Publishes a new version and makes it current immediately —
        /// Admin only.</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TenantTermsVersionEntry>> PublishVersion(PublishTenantTermsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Version))
                return BadRequest("Enter a version label (e.g. \"v2.1\").");
            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest("Terms content can't be empty.");

            int companyId = GetCallerCompanyId();
            var admin = await _masterDb.Admins
                .Include(a => a.ApplicationUser)
                .FirstOrDefaultAsync(a => a.ApplicationUser.CompanyId == companyId);
            if (admin is null)
                return BadRequest("Could not find your own Admin record.");

            // Only one version is ever current — unset whichever one holds it now.
            var currentVersions = await _tenantDb.TermsVersions.Where(t => t.IsCurrent).ToListAsync();
            foreach (var v in currentVersions)
                v.IsCurrent = false;

            var newVersion = new TenantTermsVersion
            {
                AdminId = admin.AdminId,
                Version = request.Version.Trim(),
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                IsCurrent = true
            };
            _tenantDb.TermsVersions.Add(newVersion);
            await _tenantDb.SaveChangesAsync();

            return Ok(new TenantTermsVersionEntry(newVersion.TermsId, newVersion.Version, newVersion.Content, newVersion.CreatedAt, newVersion.IsCurrent));
        }

        /// <summary>Makes an already-published (older) version current again —
        /// e.g. rolling back after a bad edit — without needing to republish it
        /// as a "new" version. Admin only.</summary>
        [HttpPut("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetCurrent(int id)
        {
            var target = await _tenantDb.TermsVersions.FirstOrDefaultAsync(t => t.TermsId == id);
            if (target is null)
                return NotFound("No version found with that id.");

            if (!target.IsCurrent)
            {
                var currentVersions = await _tenantDb.TermsVersions.Where(t => t.IsCurrent).ToListAsync();
                foreach (var v in currentVersions)
                    v.IsCurrent = false;

                target.IsCurrent = true;
                await _tenantDb.SaveChangesAsync();
            }

            return Ok();
        }

        private int GetCallerCompanyId()
        {
            string? companyIdClaim = User.FindFirst("CompanyId")?.Value;
            return int.TryParse(companyIdClaim, out var companyId) ? companyId : 0;
        }
    }
}
