using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// Superadmin's Subscriptions page: every active tenant, its billing cycle,
    /// and whether it's allowed to log in right now. Suspending a tenant flips
    /// its Admin ApplicationUser's Status — the exact same mechanism that
    /// already blocks a suspended Staff/Manager from logging in — so it's a
    /// real access-control action, not just a cosmetic label.
    /// </summary>
    [ApiController]
    [Route("api/superadmin/subscriptions")]
    [Authorize(Roles = "Superadmin")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly MasterCRMSDbContext _masterDb;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubscriptionsController(MasterCRMSDbContext masterDb, UserManager<ApplicationUser> userManager)
        {
            _masterDb = masterDb;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<List<TenantSubscriptionEntry>>> GetSubscriptions()
        {
            var companies = await _masterDb.Companies.Where(c => c.IsActive).OrderBy(c => c.CompanyName).ToListAsync();

            var result = new List<TenantSubscriptionEntry>();
            foreach (var company in companies)
            {
                var adminUser = await FindAdminUserForCompany(company.CompanyId);
                if (adminUser is null) continue; // no Admin account provisioned for this tenant yet

                var adminRow = await _masterDb.Admins.FirstOrDefaultAsync(a => a.ApplicationUserId == adminUser.Id);

                result.Add(new TenantSubscriptionEntry(
                    company.CompanyId, company.CompanyCode, company.CompanyName,
                    adminUser.UserName ?? "", adminRow?.Name ?? adminUser.FullName,
                    adminUser.Status,
                    adminRow?.BillingCycle ?? "Monthly",
                    adminRow?.NextBillingDate,
                    adminRow?.SubscriptionAmount,
                    adminRow?.SubscriptionPlan ?? "AllAccess",
                    company.CreateAt));
            }

            return Ok(result);
        }

        [HttpPut("{companyId:int}/status")]
        public async Task<IActionResult> SetStatus(int companyId, SetSubscriptionStatusRequest request)
        {
            if (request.Status is not ("Active" or "Suspended"))
                return BadRequest("Status must be Active or Suspended.");

            var adminUser = await FindAdminUserForCompany(companyId);
            if (adminUser is null)
                return NotFound("No Admin account found for this tenant.");

            adminUser.Status = request.Status;
            await _userManager.UpdateAsync(adminUser);

            var adminRow = await GetOrCreateAdminRow(adminUser);
            adminRow.SubscriptionStatus = request.Status;
            await _masterDb.SaveChangesAsync();

            return Ok($"Tenant is now {request.Status}.");
        }

        [HttpPut("{companyId:int}/billing-cycle")]
        public async Task<IActionResult> SetBillingCycle(int companyId, SetBillingCycleRequest request)
        {
            if (request.BillingCycle is not ("Monthly" or "Yearly"))
                return BadRequest("Billing cycle must be Monthly or Yearly.");

            var adminUser = await FindAdminUserForCompany(companyId);
            if (adminUser is null)
                return NotFound("No Admin account found for this tenant.");

            var adminRow = await GetOrCreateAdminRow(adminUser);
            adminRow.BillingCycle = request.BillingCycle;
            adminRow.NextBillingDate = request.NextBillingDate;
            await _masterDb.SaveChangesAsync();

            return Ok("Billing cycle updated.");
        }

        [HttpPut("{companyId:int}/amount")]
        public async Task<IActionResult> SetAmount(int companyId, SetSubscriptionAmountRequest request)
        {
            if (request.Amount is < 0)
                return BadRequest("Amount can't be negative.");

            var adminUser = await FindAdminUserForCompany(companyId);
            if (adminUser is null)
                return NotFound("No Admin account found for this tenant.");

            var adminRow = await GetOrCreateAdminRow(adminUser);
            adminRow.SubscriptionAmount = request.Amount;
            await _masterDb.SaveChangesAsync();

            return Ok("Subscription amount updated.");
        }

        [HttpPut("{companyId:int}/plan")]
        public async Task<IActionResult> SetPlan(int companyId, SetSubscriptionPlanRequest request)
        {
            if (request.Plan is not ("AllAccess" or "Growth" or "Starter"))
                return BadRequest("Plan must be AllAccess, Growth, or Starter.");

            var adminUser = await FindAdminUserForCompany(companyId);
            if (adminUser is null)
                return NotFound("No Admin account found for this tenant.");

            var adminRow = await GetOrCreateAdminRow(adminUser);
            adminRow.SubscriptionPlan = request.Plan;
            await _masterDb.SaveChangesAsync();

            return Ok("Subscription plan updated.");
        }

        // A tenant has exactly one Admin account by design, but nothing in the
        // schema marks it directly — this finds it by role instead of assuming
        // "the first ApplicationUser with this CompanyId" (Staff/Manager also
        // carry a CompanyId).
        private async Task<ApplicationUser?> FindAdminUserForCompany(int companyId)
        {
            var candidates = await _userManager.Users.Where(u => u.CompanyId == companyId).ToListAsync();
            foreach (var user in candidates)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                    return user;
            }
            return null;
        }

        // The Admins table row is optional bookkeeping (billing cycle, display
        // name) layered on top of the real ApplicationUser — older tenants
        // provisioned before this feature existed won't have one yet, so the
        // first Superadmin write against them creates it instead of failing.
        private async Task<Admin> GetOrCreateAdminRow(ApplicationUser adminUser)
        {
            var existing = await _masterDb.Admins.FirstOrDefaultAsync(a => a.ApplicationUserId == adminUser.Id);
            if (existing != null) return existing;

            string shopName = adminUser.FullName;
            if (adminUser.CompanyId.HasValue)
            {
                var company = await _masterDb.Companies.FindAsync(adminUser.CompanyId.Value);
                if (company != null) shopName = company.CompanyName;
            }

            var row = new Admin
            {
                ApplicationUserId = adminUser.Id,
                Name = adminUser.FullName,
                ShopName = shopName,
                SubscriptionStatus = adminUser.Status,
                BillingCycle = "Monthly"
            };
            _masterDb.Admins.Add(row);
            return row;
        }
    }
}
