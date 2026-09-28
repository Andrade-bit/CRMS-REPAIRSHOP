using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CRMS.domain.Entities;

namespace CRMS.infrastructure.Data
{
    public class MasterCRMSDbContext : IdentityDbContext<ApplicationUser>
    {
        // ----- Pre-existing (SaaS/tenant) entities -----
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CompanyDatabase> CompanyDatabases => Set<CompanyDatabase>();
        public DbSet<Device> Devices { get; set; }

        // ----- Core ERD entities that stay in MasterDB -----
        public DbSet<Superadmin> Superadmins => Set<Superadmin>();
        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<TermsAgreement> TermsAgreements => Set<TermsAgreement>();

        // Set true only by MasterSyncBackgroundService while it's writing back
        // the result of a successful push — without this, SaveChanges's own
        // auto-dirty logic below would immediately re-flag the row it just
        // finished syncing as unsynced again. Same pattern as TenantCRMSDbContext.
        public bool SuppressSyncDirtyTracking { get; set; }

        public MasterCRMSDbContext(DbContextOptions<MasterCRMSDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ===================== Pre-existing entities =====================

            builder.Entity<Company>(entity =>
            {
                entity.HasKey(x => x.CompanyId);

                entity.Property(x => x.CompanyCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.CompanyName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.HasIndex(x => x.CompanyCode)
                    .IsUnique();
            });
            ConfigureSyncColumns(builder.Entity<Company>());

            builder.Entity<CompanyDatabase>(entity =>
            {
                entity.HasKey(x => x.CompanyDatabaseId);

                entity.Property(x => x.ServerName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.DatabaseName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.ConnectionString)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.HasOne(x => x.Company)
                    .WithMany()
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Device>(entity =>
            {
                entity.HasKey(x => x.DeviceId);

                entity.Property(x => x.DeviceCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.DeviceName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.HasOne(x => x.Company)
                    .WithMany(x => x.Devices)
                    .HasForeignKey(x => x.CompanyID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.CompanyID, x.DeviceName })
                    .IsUnique();
            });

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(x => x.Company)
                    .WithMany()
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.IsSynced).HasDefaultValue(false);
                entity.Property(x => x.SyncedAt);
            });

            // ===================== Core ERD entities (Master-only) =====================

            builder.Entity<Superadmin>(entity =>
            {
                entity.HasKey(x => x.SuperadminId);

                entity.HasOne(x => x.ApplicationUser)
                    .WithMany()
                    .HasForeignKey(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.ApplicationUserId).IsUnique();
            });
            ConfigureSyncColumns(builder.Entity<Superadmin>());

            builder.Entity<Admin>(entity =>
            {
                entity.HasKey(x => x.AdminId);

                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.ShopName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Address).HasMaxLength(300);
                entity.Property(x => x.SubscriptionStatus).HasMaxLength(50);
                entity.Property(x => x.BillingCycle).HasMaxLength(20);
                entity.Property(x => x.SubscriptionAmount).HasColumnType("decimal(10,2)");
                entity.Property(x => x.SubscriptionPlan).HasMaxLength(20);

                entity.HasOne(x => x.ApplicationUser)
                    .WithMany()
                    .HasForeignKey(x => x.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.ApplicationUserId).IsUnique();

                entity.HasOne(x => x.Superadmin)
                    .WithMany(x => x.Admins)
                    .HasForeignKey(x => x.SuperadminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            ConfigureSyncColumns(builder.Entity<Admin>());

            builder.Entity<TermsAgreement>(entity =>
            {
                entity.HasKey(x => x.TermsId);

                entity.Property(x => x.Version).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Content).HasMaxLength(4000);
                entity.Property(x => x.PlanScope).HasMaxLength(20).IsRequired();

                entity.HasOne(x => x.Superadmin)
                    .WithMany(x => x.TermsAgreements)
                    .HasForeignKey(x => x.SuperadminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            ConfigureSyncColumns(builder.Entity<TermsAgreement>());
        }

        private static void ConfigureSyncColumns<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> entity)
            where T : class, ISyncTracked
        {
            entity.Property(x => x.IsSynced).HasDefaultValue(false);
            entity.Property(x => x.CloudId);
            entity.Property(x => x.SyncedAt);
        }

        /// <summary>Every add/edit to a synced entity type automatically flags it
        /// unsynced — same pattern as TenantCRMSDbContext. ApplicationUser can't
        /// implement ISyncTracked (its Id is a string, not the int CloudId that
        /// interface expects), so it's checked separately here.</summary>
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            MarkChangedSyncEntitiesDirty();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            MarkChangedSyncEntitiesDirty();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void MarkChangedSyncEntitiesDirty()
        {
            if (SuppressSyncDirtyTracking) return;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;

                if (entry.Entity is ISyncTracked synced)
                    synced.IsSynced = false;
                else if (entry.Entity is ApplicationUser user)
                    user.IsSynced = false;
            }
        }
    }
}