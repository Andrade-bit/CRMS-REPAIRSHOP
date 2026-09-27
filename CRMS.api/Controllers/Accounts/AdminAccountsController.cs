using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using CRMS.infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static CRMS.api.ContactValidation;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// The real, authenticated account-management endpoints Admin's User
    /// Management page uses — replaces the old unauthenticated register-temp/
    /// users/reset-password-temp endpoints for everyday use. An Admin here can
    /// only ever see, create, or manage accounts within their OWN tenant, and
    /// can only ever create Staff or Manager — never another Admin. CompanyId
    /// always comes from the caller's own JWT claim, never from the request
    /// body, so one Admin can't act on another tenant's accounts.
    /// </summary>
    [ApiController]
    [Route("api/admin/accounts")]
    [Authorize(Roles = "Admin")]
    public class AdminAccountsController : ControllerBase
    {
        private static readonly string[] CreatableRoles = { "Staff", "Manager" };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MasterCRMSDbContext _masterDb;
        private readonly ITenantConnectionService _tenantConnectionService;

        public AdminAccountsController(
            UserManager<ApplicationUser> userManager,
            MasterCRMSDbContext masterDb,
            ITenantConnectionService tenantConnectionService)
        {
            _userManager = userManager;
            _masterDb = masterDb;
            _tenantConnectionService = tenantConnectionService;
        }

        public record UserAccountResponse(string Username, string FullName, string Role, string Status);

        /// <summary>Every account belonging to the caller's own tenant — never another one.</summary>
        [HttpGet]
        public async Task<ActionResult<List<UserAccountResponse>>> GetAccounts()
        {
            int companyId = GetCallerCompanyId();

            var users = await _userManager.Users.Where(u => u.CompanyId == companyId).ToListAsync();

            var result = new List<UserAccountResponse>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserAccountResponse(
                    user.UserName ?? string.Empty, user.FullName, roles.FirstOrDefault() ?? "", user.Status));
            }

            return Ok(result);
        }

        public record CreateAccountRequest(string Username, string Password, string FullName, string Role, int? BranchId = null);

        /// <summary>Creates a Staff or Manager account — nothing else. The account
        /// always belongs to the caller's own tenant; there's no way to pass a
        /// different CompanyId in, and Role is rejected if it isn't Staff/Manager.
        /// BranchId is optional — null means this account belongs to the tenant
        /// directly, not any specific branch.</summary>
        [HttpPost]
        public async Task<IActionResult> CreateAccount(CreateAccountRequest request)
        {
            if (!CreatableRoles.Contains(request.Role))
                return BadRequest($"An Admin can only create Staff or Manager accounts, not \"{request.Role}\".");

            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest("Full name is required.");

            if (!IsValidNameIfPresent(request.FullName))
                return BadRequest("Name can only contain letters — no numbers or symbols.");

            int companyId = GetCallerCompanyId();

            var user = new ApplicationUser
            {
                UserName = request.Username,
                FullName = request.FullName,
                Status = "Active",
                CompanyId = companyId
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(user, request.Role);

            // Staff/Manager also needs a row in their tenant's own database —
            // that's what a ServiceTicket's StaffId actually links to.
            var admin = await _masterDb.Admins
                .Include(a => a.ApplicationUser)
                .FirstOrDefaultAsync(a => a.ApplicationUser.CompanyId == companyId);

            if (admin is null)
                return BadRequest("Could not find your own Admin record — cannot create the tenant-side Staff record.");

            string tenantConnectionString;
            try
            {
                tenantConnectionString = await _tenantConnectionService.GetTenantConnectionStringAsync(companyId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"Could not resolve your tenant database: {ex.Message}");
            }

            var tenantOptions = new DbContextOptionsBuilder<TenantCRMSDbContext>()
                .UseSqlServer(tenantConnectionString)
                .Options;

            await using var tenantDb = new TenantCRMSDbContext(tenantOptions);

            if (request.BranchId.HasValue)
            {
                bool branchExists = await tenantDb.Branches.AnyAsync(b => b.BranchId == request.BranchId.Value);
                if (!branchExists)
                    return BadRequest("That branch no longer exists.");
            }

            tenantDb.Staff.Add(new Staff
            {
                ApplicationUserId = user.Id,
                AdminId = admin.AdminId,
                Name = request.FullName,
                Status = "Active",
                BranchId = request.BranchId
            });
            await tenantDb.SaveChangesAsync();

            return Ok($"Created {request.Role} account for {request.Username}.");
        }

        public record ResetPasswordRequest(string NewPassword);

        /// <summary>Resets a password, but only for an account in the caller's own
        /// tenant — an Admin can never touch another tenant's accounts, even by
        /// guessing a username.</summary>
        [HttpPut("{username}/reset-password")]
        public async Task<IActionResult> ResetPassword(string username, ResetPasswordRequest request)
        {
            int companyId = GetCallerCompanyId();

            var user = await _userManager.FindByNameAsync(username);
            if (user is null || user.CompanyId != companyId)
                return NotFound("No account with that username found in your tenant.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            return Ok($"Password updated for {username}.");
        }

        public record SetStatusRequest(string Status);
        private static readonly string[] ValidStatuses = { "Active", "Suspended" };

        /// <summary>Suspends or reactivates a Staff/Manager account in the caller's
        /// own tenant. A suspended account keeps its data but can no longer log
        /// in (enforced in AuthController.Login). Deliberately can't target
        /// another Admin — even within the same tenant — that's a Superadmin
        /// action once that role has a real backend.</summary>
        [HttpPut("{username}/status")]
        public async Task<IActionResult> SetStatus(string username, SetStatusRequest request)
        {
            if (!ValidStatuses.Contains(request.Status))
                return BadRequest($"\"{request.Status}\" is not a recognized account status.");

            int companyId = GetCallerCompanyId();

            var user = await _userManager.FindByNameAsync(username);
            if (user is null || user.CompanyId != companyId)
                return NotFound("No account with that username found in your tenant.");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(r => CreatableRoles.Contains(r)))
                return BadRequest("An Admin can only suspend or reactivate Staff or Manager accounts.");

            user.Status = request.Status;
            await _userManager.UpdateAsync(user);

            return Ok($"{username} is now {request.Status}.");
        }

        private int GetCallerCompanyId()
        {
            string? companyIdClaim = User.FindFirst("CompanyId")?.Value;
            return int.TryParse(companyIdClaim, out var companyId) ? companyId : 0;
        }
    }
}
