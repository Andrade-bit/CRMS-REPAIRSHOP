using System;
using System.Threading.Tasks;
using CRMS.infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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

        public TenantConnectionService(MasterCRMSDbContext masterDb, IConfiguration configuration)
        {
            _masterDb = masterDb;
            _configuration = configuration;
        }

        public async Task<string> GetTenantConnectionStringAsync(int companyId)
        {
            var company = await _masterDb.Companies
                .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.IsActive);

            if (company is null)
                throw new InvalidOperationException($"No active company found for CompanyId {companyId}.");

            var section = _configuration.GetSection($"TenantCredentials:{company.CompanyCode}");
            if (!section.Exists())
                throw new InvalidOperationException(
                    $"No TenantCredentials entry configured for tenant \"{company.CompanyCode}\" in appsettings.json.");

            string Require(string key) => section[key]
                ?? throw new InvalidOperationException(
                    $"TenantCredentials:{company.CompanyCode}:{key} is missing in appsettings.json.");

            string server = Require("ServerName");
            string database = Require("DatabaseName");
            string userId = Require("UserId");
            string password = Require("Password");

            return $"Server={server};Database={database};User Id={userId};Password={password};" +
                   "Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        }
    }
}
