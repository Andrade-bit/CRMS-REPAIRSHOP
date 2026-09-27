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
        private static readonly string[] ValidPlanScopes = { "All", "Starter", "Growth", "AllAccess" };

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
                .Select(t => new TermsVersionEntry(t.TermsId, t.Version, t.Content, t.CreatedAt, t.IsCurrent, t.PlanScope))
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
            if (!ValidPlanScopes.Contains(request.PlanScope))
                return BadRequest("\"" + request.PlanScope + "\" is not a recognized plan.");

            var superadmin = await GetCallerSuperadminAsync();
            if (superadmin is null)
                return BadRequest("No Superadmin record found for this account.");

            // Exactly one version is current PER plan scope — unset whichever
            // version currently holds it for THIS scope only, leaving other
            // plans' current versions untouched.
            var currentForScope = await _masterDb.TermsAgreements
                .Where(t => t.IsCurrent && t.PlanScope == request.PlanScope)
                .ToListAsync();
            foreach (var v in currentForScope)
                v.IsCurrent = false;

            var newVersion = new TermsAgreement
            {
                SuperadminId = superadmin.SuperadminId,
                Version = request.Version.Trim(),
                Content = request.Content,
                PlanScope = request.PlanScope,
                CreatedAt = DateTime.UtcNow,
                IsCurrent = true
            };
            _masterDb.TermsAgreements.Add(newVersion);
            await _masterDb.SaveChangesAsync();

            return Ok(new TermsVersionEntry(newVersion.TermsId, newVersion.Version, newVersion.Content, newVersion.CreatedAt, newVersion.IsCurrent, newVersion.PlanScope));
        }

        /// <summary>Moves an already-published version to a different plan (or
        /// to/from "All plans") without republishing it as a new version. If
        /// the version is currently the active one for its old scope, that
        /// "current" status moves with it to the new scope — bumping whichever
        /// version held "current" there before, and leaving the old scope with
        /// no current version until another one is published or edited in.</summary>
        [HttpPut("{id}/scope")]
        public async Task<ActionResult<TermsVersionEntry>> UpdateScope(int id, UpdateTermsScopeRequest request)
        {
            if (!ValidPlanScopes.Contains(request.PlanScope))
                return BadRequest("\"" + request.PlanScope + "\" is not a recognized plan.");

            var target = await _masterDb.TermsAgreements.FirstOrDefaultAsync(t => t.TermsId == id);
            if (target is null)
                return NotFound("No version found with that id.");

            if (target.PlanScope != request.PlanScope)
            {
                if (target.IsCurrent)
                {
                    var currentInNewScope = await _masterDb.TermsAgreements
                        .Where(t => t.IsCurrent && t.PlanScope == request.PlanScope && t.TermsId != id)
                        .ToListAsync();
                    foreach (var v in currentInNewScope)
                        v.IsCurrent = false;
                }

                target.PlanScope = request.PlanScope;
                await _masterDb.SaveChangesAsync();
            }

            return Ok(new TermsVersionEntry(target.TermsId, target.Version, target.Content, target.CreatedAt, target.IsCurrent, target.PlanScope));
        }

        /// <summary>The version currently in effect for a given plan — the
        /// plan-specific current version if one exists, otherwise the "All"
        /// scope's current version as a fallback. Used by the Subscriptions
        /// page to show each tenant which terms apply to them.</summary>
        [HttpGet("current/{plan}")]
        public async Task<ActionResult<TermsVersionEntry?>> GetCurrentForPlan(string plan)
        {
            var forPlan = await _masterDb.TermsAgreements.FirstOrDefaultAsync(t => t.IsCurrent && t.PlanScope == plan);
            var effective = forPlan ?? await _masterDb.TermsAgreements.FirstOrDefaultAsync(t => t.IsCurrent && t.PlanScope == "All");

            if (effective is null) return Ok(null);
            return Ok(new TermsVersionEntry(effective.TermsId, effective.Version, effective.Content, effective.CreatedAt, effective.IsCurrent, effective.PlanScope));
        }

        /// <summary>Nothing in this codebase ever creates a Superadmin row —
        /// accounts with the Superadmin role so far have all been seeded
        /// directly into Identity (AspNetUsers/AspNetUserRoles) without one.
        /// Self-heals here instead of rejecting an otherwise-valid Superadmin
        /// login: the first time such an account calls this, it gets its
        /// matching domain row created on the spot.</summary>
        private async Task<Superadmin?> GetCallerSuperadminAsync()
        {
            string? userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
            if (userId is null) return null;

            var existing = await _masterDb.Superadmins.FirstOrDefaultAsync(s => s.ApplicationUserId == userId);
            if (existing is not null) return existing;

            var superadmin = new Superadmin { ApplicationUserId = userId };
            _masterDb.Superadmins.Add(superadmin);
            await _masterDb.SaveChangesAsync();
            return superadmin;
        }
    }
}
