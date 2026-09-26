using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CRMS.domain.Entities;

namespace CRMS.infrastructure.Data
{
    // Represents ONE tenant's (repair shop's) database.
    // Which physical database this connects to is decided per-request
    // by ITenantConnectionService — see Program.cs registration.
    public class TenantCRMSDbContext : DbContext
    {
        // Set true only by TenantSyncBackgroundService while it's writing back
        // the result of a successful push (IsSynced=true, CloudId=...) — without
        // this, SaveChangesAsync's own auto-dirty logic below would immediately
        // re-flag the row it just finished syncing as unsynced again.
        public bool SuppressSyncDirtyTracking { get; set; }
        public DbSet<Staff> Staff => Set<Staff>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<ServiceTicket> ServiceTickets => Set<ServiceTicket>();
        public DbSet<RepairPart> RepairParts => Set<RepairPart>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Promotion> Promotions => Set<Promotion>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<TenantTermsVersion> TermsVersions => Set<TenantTermsVersion>();

        public TenantCRMSDbContext(DbContextOptions<TenantCRMSDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // NOTE: AdminId/ApplicationUserId fields below are plain columns only.
            // No HasOne(...) to Admin/ApplicationUser — those tables live in MasterDB,
            // a different physical database, so EF/SQL can't enforce that FK here.

            builder.Entity<Staff>(entity =>
            {
                entity.HasKey(x => x.StaffId);

                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Status).HasMaxLength(50);

                // AdminId, ApplicationUserId stay as plain int/string columns (no HasOne).

                // Branch DOES live in this same tenant DB, so it's a real FK.
                entity.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            ConfigureSyncColumns(builder.Entity<Staff>());

            builder.Entity<Customer>(entity =>
            {
                entity.HasKey(x => x.CustomerId);

                entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.PhoneNumber).HasMaxLength(30);
                entity.Property(x => x.Email).HasMaxLength(200);

                // AdminId stays as a plain int column (no HasOne).
            });

            ConfigureSyncColumns(builder.Entity<Customer>());

            builder.Entity<ServiceTicket>(entity =>
            {
                entity.HasKey(x => x.TicketId);

                entity.Property(x => x.DeviceName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Concern).HasMaxLength(1000);
                entity.Property(x => x.Status).HasMaxLength(50);
                entity.Property(x => x.PaymentStatus).HasMaxLength(50);
                entity.Property(x => x.EstimatedCost).HasColumnType("decimal(10,2)");
                entity.Property(x => x.AmountPaid).HasColumnType("decimal(10,2)");
                entity.Property(x => x.OriginalPrice).HasColumnType("decimal(10,2)");

                entity.Property(x => x.DiagnosisFindings).HasMaxLength(1000);
                entity.Property(x => x.ApprovalStatus).HasMaxLength(20).IsRequired();
                entity.Property(x => x.ApprovedAmount).HasColumnType("decimal(10,2)");
                entity.Property(x => x.ApprovalMethod).HasMaxLength(50);

                // These two ARE real FKs — Customer and Staff both live in this same tenant DB.
                entity.HasOne(x => x.Customer)
                    .WithMany(x => x.ServiceTickets)
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Staff)
                    .WithMany(x => x.ServiceTickets)
                    .HasForeignKey(x => x.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Branch)
                    .WithMany()
                    .HasForeignKey(x => x.BranchId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Optional — which approved promotion the customer used, if any.
                entity.HasOne(x => x.Promotion)
                    .WithMany()
                    .HasForeignKey(x => x.PromotionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            ConfigureSyncColumns(builder.Entity<ServiceTicket>());

            builder.Entity<RepairPart>(entity =>
            {
                entity.HasKey(x => x.PartId);

                entity.Property(x => x.PartName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.PartCost).HasColumnType("decimal(10,2)");

                entity.HasOne(x => x.Ticket)
                    .WithMany(x => x.RepairParts)
                    .HasForeignKey(x => x.TicketId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Payment>(entity =>
            {
                entity.HasKey(x => x.PaymentId);

                entity.Property(x => x.PaymentType).HasMaxLength(50);
                entity.Property(x => x.Amount).HasColumnType("decimal(10,2)");

                entity.HasOne(x => x.Ticket)
                    .WithMany(x => x.Payments)
                    .HasForeignKey(x => x.TicketId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Feedback>(entity =>
            {
                entity.HasKey(x => x.FeedbackId);

                entity.Property(x => x.Message).HasMaxLength(1000);

                entity.HasOne(x => x.Ticket)
                    .WithMany(x => x.Feedbacks)
                    .HasForeignKey(x => x.TicketId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Feedback>()
                .Property(x => x.Rating)
                .HasDefaultValue(5);

            builder.Entity<Notification>(entity =>
            {
                entity.HasKey(x => x.NotificationId);

                entity.Property(x => x.Channel).HasMaxLength(50);
                entity.Property(x => x.Message).HasMaxLength(500);

                entity.HasOne(x => x.Ticket)
                    .WithMany(x => x.Notifications)
                    .HasForeignKey(x => x.TicketId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Promotion>(entity =>
            {
                entity.HasKey(x => x.PromotionId);

                entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Type).HasMaxLength(50);
                entity.Property(x => x.Message).HasMaxLength(500);
                entity.Property(x => x.Reason).HasMaxLength(500);
                entity.Property(x => x.Status).HasMaxLength(50);

                // Staff lives in this same tenant DB, so this is a real FK.
                entity.HasOne(x => x.Staff)
                    .WithMany()
                    .HasForeignKey(x => x.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);

                // ReviewedByAdminId stays a plain nullable int column (no HasOne — Admin lives in MasterDB).
            });

            ConfigureSyncColumns(builder.Entity<Promotion>());

            builder.Entity<Branch>(entity =>
            {
                entity.HasKey(x => x.BranchId);
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Address).HasMaxLength(300);
                entity.Property(x => x.Phone).HasMaxLength(30);
                entity.Property(x => x.Email).HasMaxLength(200);
            });

            ConfigureSyncColumns(builder.Entity<Branch>());

            builder.Entity<TenantTermsVersion>(entity =>
            {
                entity.HasKey(x => x.TermsId);
                entity.Property(x => x.Version).HasMaxLength(16).IsRequired();
                entity.Property(x => x.Content).IsRequired();

                // AdminId stays a plain int column — Admin lives in MasterDB.
            });
        }

        private static void ConfigureSyncColumns<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> entity)
            where T : class, ISyncTracked
        {
            entity.Property(x => x.IsSynced).HasDefaultValue(false);
            entity.Property(x => x.CloudId);
            entity.Property(x => x.SyncedAt);
        }

        /// <summary>
        /// Every add/edit to a synced entity type automatically flags it
        /// unsynced — no controller anywhere has to remember to do this by
        /// hand, including ones written after this feature existed. The sync
        /// worker is the one exception: it sets SuppressSyncDirtyTracking
        /// while writing back a successful push's own IsSynced=true/CloudId,
        /// so it doesn't immediately undo the very state it just set.
        /// </summary>
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
                if (entry.Entity is ISyncTracked synced &&
                    entry.State is EntityState.Added or EntityState.Modified)
                {
                    synced.IsSynced = false;
                }
            }
        }
    }
}