using CRMS.api.Services;
using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using CRMS.infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// launchSettings.json (the previous source of this) only applies under
// `dotnet run`/Visual Studio — a published exe has no launchSettings, so the
// port has to be pinned here or the WinForms client's hardcoded
// http://localhost:5029 would have nothing listening on it.
builder.WebHost.UseUrls("http://localhost:5029");

// EnableRetryOnFailure: MonsterASP's hosted SQL Server occasionally answers
// slowly (idle wake-up, transient network blips) — without this, one slow
// handshake throws and crashes the whole request (or, at startup, the app).
builder.Services.AddDbContext<MasterCRMSDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MasterCRMS"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITenantConnectionService, TenantConnectionService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Local-first cloud sync: a singleton in-memory status board the background
// worker writes to and the /api/sync/status endpoint reads from, plus the
// worker itself — runs continuously for the lifetime of the API process.
builder.Services.AddSingleton<ITenantSyncStatusStore, TenantSyncStatusStore>();
builder.Services.AddHostedService<TenantSyncBackgroundService>();

// TenantCRMSDbContext: connection string is resolved per-request from the
// logged-in user's CompanyId claim, via ITenantConnectionService.
builder.Services.AddDbContext<TenantCRMSDbContext>((serviceProvider, options) =>
{
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    var companyIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("CompanyId")?.Value;

    if (int.TryParse(companyIdClaim, out var companyId))
    {
        var tenantService = serviceProvider.GetRequiredService<ITenantConnectionService>();
        var connectionString = tenantService
            .GetTenantConnectionStringAsync(companyId)
            .GetAwaiter().GetResult();

        options.UseSqlServer(connectionString,
            sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null));
    }
    else
    {
        options.UseSqlServer("Server=none;Database=none;");
    }
});

// ----- Identity: users + roles, backed by MasterCRMSDbContext -----
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Keep default password rules for now; relax/tighten later as needed.
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
    .AddEntityFrameworkStores<MasterCRMSDbContext>()
    .AddDefaultTokenProviders();

// ----- JWT auth: WinForms calls the API over HTTP, so it authenticates with a bearer token -----
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    //This runs before any controller code, on every request. It pulls CompanyId straight out of the already-verified token,
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// One-time role seeding at startup: makes sure Superadmin/Admin/Staff/Customer
// roles always exist, so you never have to create them manually.
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Superadmin", "Admin", "Staff", "Manager" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

// No HTTPS binding is configured (see UseUrls above) — this API only ever
// talks to the WinForms client on the same machine over http://localhost,
// so there's nothing here for a redirect to upgrade to.
app.UseAuthentication(); // must come before UseAuthorization
app.UseAuthorization();

app.MapControllers();




app.Run();