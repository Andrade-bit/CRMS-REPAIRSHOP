using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS.domain.Entities
{
    public class CompanyDatabase
    {
        public int CompanyDatabaseId { get; set; }

        public int CompanyId { get; set; }

        public string ServerName { get; set; } = string.Empty;

        public string DatabaseName { get; set; } = string.Empty;

        // Full connection string to the tenant's database (includes credentials).
        // Simple for now — later you can encrypt this or move credentials elsewhere.
        public string ConnectionString { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public Company? Company { get; set; }
    }
}