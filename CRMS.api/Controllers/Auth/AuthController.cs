using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using CRMS.infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CRMS.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly MasterCRMSDbContext _masterDb;
        private readonly ITenantConnectionService _tenantConnectionService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config,
            MasterCRMSDbContext masterDb,
            ITenantConnectionService tenantConnectionService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _masterDb = masterDb;
            _tenantConnectionService = tenantConnectionService;
        }

        public record LoginRequest(string Username, string Password);
        public record LoginResponse(string Token, string FullName, IList<string> Roles, int? CompanyId);

        // DEV BOOTSTRAP ONLY — the real app no longer calls this. Admin's User
        // Management page now uses AdminAccountsController (authenticated,
        // Staff/Manager only, tenant-scoped). This one stays intentionally
        // unauthenticated and able to create ANY role, including Admin — that's
        // still needed to hand-create the first Admin account for a brand-new
        // tenant until a real "Superadmin creates a shop" flow exists. Never
        // called from the WinForms app; only run this yourself, by hand.
        // CompanyId is required for an Admin account — it's what the JWT's
        // "CompanyId" claim carries, which is how TenantConnectionService knows
        // which tenant's hardcoded appsettings.json credentials to use for them.
        public record RegisterRequest(string Username, string Password, string FullName, string Role, int? CompanyId = null);

        [HttpPost("register-temp")]
        public async Task<IActionResult> RegisterTemp(RegisterRequest request)
        {
            var user = new ApplicationUser
            {
                UserName = request.Username,
                FullName = request.FullName,
                Status = "Active",
                CompanyId = request.CompanyId
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(user, request.Role);

            // A Staff/Manager account also needs a row in their tenant's own
            // database — that's what a ServiceTicket's StaffId actually links
            // to (tickets live per-tenant, logins live centrally). Without
            // this, a newly created Staff account could log in but couldn't
            // be attributed to any ticket they create.
            if (request.CompanyId.HasValue && (request.Role == "Staff" || request.Role == "Manager"))
            {
                var admin = await _masterDb.Admins
                    .Include(a => a.ApplicationUser)
                    .FirstOrDefaultAsync(a => a.ApplicationUser.CompanyId == request.CompanyId);

                if (admin is null)
                    return BadRequest($"No Admin found for CompanyId {request.CompanyId} — cannot create the tenant-side Staff record.");

                string tenantConnectionString;
                try
                {
                    tenantConnectionString = await _tenantConnectionService.GetTenantConnectionStringAsync(request.CompanyId.Value);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest($"Could not resolve tenant database for CompanyId {request.CompanyId}: {ex.Message}");
                }

                var tenantOptions = new DbContextOptionsBuilder<TenantCRMSDbContext>()
                    .UseSqlServer(tenantConnectionString)
                    .Options;

                await using var tenantDb = new TenantCRMSDbContext(tenantOptions);
                tenantDb.Staff.Add(new Staff
                {
                    ApplicationUserId = user.Id,
                    AdminId = admin.AdminId,
                    Name = request.FullName,
                    Status = "Active"
                });
                await tenantDb.SaveChangesAsync();
            }

            return Ok($"Created {request.Role} account for {request.Username}.");
        }

        public record UserAccountResponse(string Username, string FullName, string Role, string Status);

        // DEV BOOTSTRAP ONLY — same reasoning as register-temp above. The real
        // app uses AdminAccountsController.GetAccounts instead (tenant-scoped
        // from the caller's own token, not an arbitrary companyId query param).
        [HttpGet("users")]
        public async Task<ActionResult<List<UserAccountResponse>>> GetUsers([FromQuery] int? companyId)
        {
            var query = _userManager.Users.AsQueryable();
            if (companyId.HasValue)
                query = query.Where(u => u.CompanyId == companyId);

            var users = await query.ToListAsync();

            var result = new List<UserAccountResponse>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserAccountResponse(
                    user.UserName ?? string.Empty, user.FullName, roles.FirstOrDefault() ?? "", user.Status));
            }

            return Ok(result);
        }

        // DEV BOOTSTRAP ONLY — same reasoning as register-temp above. The real
        // app uses AdminAccountsController.ResetPassword instead (tenant-scoped,
        // requires an authenticated Admin).
        public record ResetPasswordRequest(string Username, string NewPassword);

        [HttpPost("reset-password-temp")]
        public async Task<IActionResult> ResetPasswordTemp(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user is null)
                return NotFound("User not found.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            return Ok($"Password updated for {request.Username}.");
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user is null)
                return Unauthorized("Invalid username or password.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
                return Unauthorized("Invalid username or password.");

            // A suspended account has a correct password but should still be
            // refused — Status existed before this check but nothing ever read
            // it, so "suspending" someone didn't actually stop them logging in.
            if (user.Status != "Active")
                return Unauthorized("This account has been suspended. Contact your shop admin.");

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            return Ok(new LoginResponse(token, user.FullName, roles, user.CompanyId));
        }

        // Sub (the user's id) — this is how the app later figures out which staff member is performing an action (more below).
        //Role — lets [Authorize]/role checks gate what an account is even allowed to call.
        //CompanyId — this is the one that makes multi-tenancy automatic. It's only added if the account has one (Superadmin accounts don't belong to a company, so their token simply has no CompanyId claim).
        //
        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var jwtSection = _config.GetSection("Jwt");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            if (user.CompanyId.HasValue)
            {
                claims.Add(new Claim("CompanyId", user.CompanyId.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}