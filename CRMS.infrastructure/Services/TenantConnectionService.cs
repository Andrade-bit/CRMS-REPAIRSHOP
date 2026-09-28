using System;
using System.Threading.Tasks;
using CRMS.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace CRMS.infrastructure.Services
{
    // Tenant DB credentials are hardcoded per tenant in appsettings.json
    // ("TenantCredentials:<CompanyCode>"), not stored in a database table.
    // Which tenant a logged-in Admin belongs to is still resolved automatically
    // from their CompanyId claim -> Companies.CompanyCode -> that config block,
    // so nothing has to be switched by hand between logins.
    public class TenantConnectionService : ITenantConnectionService
    {
        private readonly MasterCRMSDbContext _masterDb;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        // Every request needs this connection string, but the answer for a given
        // CompanyId almost never changes — without caching, every ticket/customer/
        // promotion request paid for an extra round trip to the Master DB just to
        // re-derive it, on top of the tenant DB call that does the real work.
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public TenantConnectionService(MasterCRMSDbContext masterDb, IConfiguration configuration, IMemoryCache cache)
        {
            _masterDb = masterDb;
            _configuration = configuration;
            _cache = cache;
        }

        public Task<string> GetTenantConnectionStringAsync(int companyId) =>
            ResolveConnectionStringAsync(companyId, "TenantCredentials", "TenantConnectionString");

        public Task<string> GetTenantCloudConnectionStringAsync(int companyId) =>
            ResolveConnectionStringAsync(companyId, "TenantCloudCredentials", "TenantCloudConnectionString");

        public string? GetMasterCloudConnectionString() =>
            _configuration.GetConnectionString("MasterCloud");

        private async Task<string> ResolveConnectionStringAsync(int companyId, string configSectionRoot, string cacheKeyPrefix)
        {
            string cacheKey = $"{cacheKeyPrefix}:{companyId}";
            if (_cache.TryGetValue(cacheKey, out string? cachedConnectionString) && cachedConnectionString is not null)
                return cachedConnectionString;

            var company = await _masterDb.Companies
                .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.IsActive);

            if (company is null)
                throw new InvalidOperationException($"No active company found for CompanyId {companyId}.");

            var section = _configuration.GetSection($"{configSectionRoot}:{company.CompanyCode}");
            if (!section.Exists())
                throw new InvalidOperationException(
                    $"No {configSectionRoot} entry configured for tenant \"{company.CompanyCode}\" in appsettings.json.");

            string Require(string key) => section[key]
                ?? throw new InvalidOperationException(
                    $"{configSectionRoot}:{company.CompanyCode}:{key} is missing in appsettings.json.");

            string server = Require("ServerName");
            string database = Require("DatabaseName");

            // Local dev databases (e.g. LocalDB) use Windows-integrated auth, not a
            // SQL login — leaving UserId blank in config selects that mode instead
            // of the cloud tenants' User Id/Password style.
            string userId = section["UserId"] ?? string.Empty;

            string connectionString = string.IsNullOrWhiteSpace(userId)
                ? $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
                : $"Server={server};Database={database};User Id={userId};Password={Require("Password")};" +
                  "Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

            _cache.Set(cacheKey, connectionString, CacheDuration);
            return connectionString;
        }
    }
}
