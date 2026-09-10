using CRMS.domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }

        public record LoginRequest(string Username, string Password);
        public record LoginResponse(string Token, string FullName, IList<string> Roles, int? CompanyId);

        // TEMPORARY — for creating your first accounts only. Delete this endpoint
        // once you have real staff/admin accounts and a proper "create staff" flow
        // built (which should require an Admin to be logged in to call it).
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

            return Ok($"Created {request.Role} account for {request.Username}.");
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

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            return Ok(new LoginResponse(token, user.FullName, roles, user.CompanyId));
        }

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