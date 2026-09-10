using Microsoft.EntityFrameworkCore;
using CRMS.domain.Entities;

namespace CRMS.infrastructure.Data
{
    // Represents ONE tenant's (repair shop's) database.
    // Which physical database this connects to is decided per-request
    // by ITenantConnectionService — see Program.cs registration.
    public class TenantCRMSDbContext : DbContext
    {
        public DbSet<Staff> Staff => Set<Staff>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<ServiceTicket> ServiceTickets => Set<ServiceTicket>();
        public DbSet<RepairPart> RepairParts => Set<RepairPart>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Promotion> Promotions => Set<Promotion>();

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
            });

            builder.Entity<Customer>(entity =>
            {
                entity.HasKey(x => x.CustomerId);

                entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.PhoneNumber).HasMaxLength(30);
                entity.Property(x => x.Email).HasMaxLength(200);

                // AdminId stays as a plain int column (no HasOne).
            });

            builder.Entity<ServiceTicket>(entity =>
            {
                entity.HasKey(x => x.TicketId);

                entity.Property(x => x.DeviceName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Concern).HasMaxLength(1000);
                entity.Property(x => x.Status).HasMaxLength(50);
                entity.Property(x => x.PaymentStatus).HasMaxLength(50);
                entity.Property(x => x.EstimatedCost).HasColumnType("decimal(10,2)");

                // These two ARE real FKs — Customer and Staff both live in this same tenant DB.
                entity.HasOne(x => x.Customer)
                    .WithMany(x => x.ServiceTickets)
                    .HasForeignKey(x => x.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Staff)
                    .WithMany(x => x.ServiceTickets)
                    .HasForeignKey(x => x.StaffId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

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

                entity.Property(x => x.Message).HasMaxLength(500);

                // AdminId stays a plain int column (no HasOne — Admin lives in MasterDB).
            });
        }
    }
}