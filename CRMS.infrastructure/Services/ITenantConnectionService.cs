using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS.infrastructure.Services
{
    public interface ITenantConnectionService
    {
        Task<string> GetTenantConnectionStringAsync(int companyId);
    }
}
