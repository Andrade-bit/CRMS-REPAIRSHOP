using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS.infrastructure.Services
{
    public interface ITenantConnectionService
    {
        Task<string> GetTenantConnectionStringAsync(int companyId);

        /// <summary>The cloud database this tenant's local-first sync worker
        /// pushes to — read from "TenantCloudCredentials:&lt;CompanyCode&gt;",
        /// a separate config section from the primary "TenantCredentials" one
        /// (which is always the tenant's LOCAL/primary connection). Throws if
        /// no cloud credentials are configured for this tenant, since a tenant
        /// with nothing to sync to should never have been scheduled to sync.</summary>
        Task<string> GetTenantCloudConnectionStringAsync(int companyId);
    }
}
