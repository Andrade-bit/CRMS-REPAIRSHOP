using CRMS.infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using CRMS.infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMS.infrastructure.Services
{
    public class TenantConnectionService : ITenantConnectionService
    {
        private readonly MasterCRMSDbContext _masterDb;

        public TenantConnectionService(MasterCRMSDbContext masterDb)
        {
            _masterDb = masterDb;
        }

        public async Task<string> GetTenantConnectionStringAsync(int companyId)
        {
            var dbRecord = await _masterDb.CompanyDatabases
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive);

            if (dbRecord is null)
                throw new InvalidOperationException(
                    $"No active tenant database configured for CompanyId {companyId}.");

            return dbRecord.ConnectionString;
        }
    }
}
