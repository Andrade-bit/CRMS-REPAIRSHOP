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

            builder.Entity<TermsAgreement>(entity =>
            {
                entity.HasKey(x => x.TermsId);

                entity.Property(x => x.Version).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Content).HasMaxLength(4000);

                entity.HasOne(x => x.Superadmin)
                    .WithMany(x => x.TermsAgreements)
                    .HasForeignKey(x => x.SuperadminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}