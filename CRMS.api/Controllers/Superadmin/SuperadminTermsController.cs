using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// Superadmin's Terms &amp; Agreement page: version history, and publishing a
    /// new version. Exactly one version is ever "current" — publishing a new
    /// one flips the flag off whichever version held it before.
    /// </summary>
    [ApiController]
    [Route("api/superadmin/terms")]
    [Authorize(Roles = "Superadmin")]
    public class SuperadminTermsController : ControllerBase
    {
        private readonly MasterCRMSDbContext _masterDb;

        public SuperadminTermsController(MasterCRMSDbContext masterDb)
        {
            _masterDb = masterDb;
        }

        [HttpGet]
        public async Task<ActionResult<List<TermsVersionEntry>>> GetVersions()
        {
            var versions = await _masterDb.TermsAgreements
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TermsVersionEntry(t.TermsId, t.Version, t.Content, t.CreatedAt, t.IsCurrent))
                .ToListAsync();

            return Ok(versions);
        }

        [HttpPost]
        public async Task<ActionResult<TermsVersionEntry>> PublishVersion(PublishTermsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Version))
                return BadRequest("Enter a version label (e.g. \"2.2\").");
            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest("Terms content can't be empty.");

            var superadmin = await GetCallerSuperadminAsync();
            if (superadmin is null)
                return BadRequest("No Superadmin record found for this account.");

            // Only one version is ever current — unset whichever one holds it now.
            var currentVersions = await _masterDb.TermsAgreements.Where(t => t.IsCurrent).ToListAsync();
            foreach (var v in currentVersions)
                v.IsCurrent = false;

            var newVersion = new TermsAgreement
            {
                SuperadminId = superadmin.SuperadminId,
                Version = request.Version.Trim(),
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                IsCurrent = true
            };
            _masterDb.TermsAgreements.Add(newVersion);
            await _masterDb.SaveChangesAsync();

            return Ok(new TermsVersionEntry(newVersion.TermsId, newVersion.Version, newVersion.Content, newVersion.CreatedAt, newVersion.IsCurrent));
        }

        private async Task<Superadmin?> GetCallerSuperadminAsync()
        {
            string? userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
            if (userId is null) return null;

            return await _masterDb.Superadmins.FirstOrDefaultAsync(s => s.ApplicationUserId == userId);
        }
    }
}
