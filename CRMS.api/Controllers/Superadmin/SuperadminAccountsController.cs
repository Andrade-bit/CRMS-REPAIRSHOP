using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// Superadmin's User Management page: the one Admin account per tenant.
    /// Deliberately does not create new Admin accounts (tenant provisioning is
    /// its own separate, deferred feature) — this only manages the accounts
    /// that already exist: reset a forgotten password, suspend/reactivate.
    /// </summary>
    [ApiController]
    [Route("api/superadmin/admins")]
    [Authorize(Roles = "Superadmin")]
    public class SuperadminAccountsController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MasterCRMSDbContext _masterDb;

        public SuperadminAccountsController(UserManager<ApplicationUser> userManager, MasterCRMSDbContext masterDb)
        {
            _userManager = userManager;
            _masterDb = masterDb;
        }

        [HttpGet]
        public async Task<ActionResult<List<SuperadminAdminAccountEntry>>> GetAdminAccounts()
        {
            var companies = await _masterDb.Companies.Where(c => c.IsActive).ToDictionaryAsync(c => c.CompanyId, c => c.CompanyName);

            var admins = await _userManager.Users.Where(u => u.CompanyId != null).ToListAsync();

            var result = new List<SuperadminAdminAccountEntry>();
            foreach (var user in admins)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Admin")) continue;
                if (!user.CompanyId.HasValue || !companies.TryGetValue(user.CompanyId.Value, out var companyName)) continue;

                result.Add(new SuperadminAdminAccountEntry(user.CompanyId.Value, companyName, user.UserName ?? "", user.FullName, user.Status));
            }

            return Ok(result.OrderBy(a => a.CompanyName).ToList());
        }

        [HttpPut("{username}/reset-password")]
        public async Task<IActionResult> ResetPassword(string username, SuperadminResetPasswordRequest request)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user is null) return NotFound("No account with that username found.");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin"))
                return BadRequest("This endpoint only resets an Admin account's password.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            return Ok($"Password updated for {username}.");
        }

        public record SetStatusRequest(string Status);

        [HttpPut("{username}/status")]
        public async Task<IActionResult> SetStatus(string username, SetStatusRequest request)
        {
            if (request.Status is not ("Active" or "Suspended"))
                return BadRequest("Status must be Active or Suspended.");

            var user = await _userManager.FindByNameAsync(username);
            if (user is null) return NotFound("No account with that username found.");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin"))
                return BadRequest("This endpoint only suspends/reactivates an Admin account.");

            user.Status = request.Status;
            await _userManager.UpdateAsync(user);

            var adminRow = await _masterDb.Admins.FirstOrDefaultAsync(a => a.ApplicationUserId == user.Id);
            if (adminRow != null)
            {
                adminRow.SubscriptionStatus = request.Status;
                await _masterDb.SaveChangesAsync();
            }

            return Ok($"{username} is now {request.Status}.");
        }
    }
}
