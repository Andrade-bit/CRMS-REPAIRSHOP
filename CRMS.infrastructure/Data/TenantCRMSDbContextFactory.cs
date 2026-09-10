using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CRMS.infrastructure.Data
{
    // Used ONLY by EF Core tooling (Add-Migration / Update-Database) at design time.
    // The connection string here is a placeholder — the real one is resolved
    // per-request at runtime in Program.cs via ITenantConnectionService.
    // When you pass -Connection to Update-Database, it overrides this placeholder.
    public class TenantCRMSDbContextFactory : IDesignTimeDbContextFactory<TenantCRMSDbContext>
    {
        public TenantCRMSDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TenantCRMSDbContext>();
            optionsBuilder.UseSqlServer("Server=localhost;Database=TenantDesignTime;TrustServerCertificate=True;");

            return new TenantCRMSDbContext(optionsBuilder.Options);
        }
    }
}