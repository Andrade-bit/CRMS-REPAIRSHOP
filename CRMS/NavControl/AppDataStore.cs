using System;
using System.Collections.Generic;
using System.Linq;

namespace CRMS.winforms.Controllers
{
    /// <summary>Ticket statuses a staff member can move a walk-in repair through.</summary>
    public static class TicketStatus
    {
        public const string Diagnose = "Diagnose";
        public const string AwaitingApproval = "AwaitingApproval";
        public const string Repairing = "Repairing";
        public const string Repaired = "Repaired";
        public const string Cancelled = "Cancelled";
        public const string Unrepairable = "Unrepairable";

        public static readonly string[] All = { Diagnose, AwaitingApproval, Repairing, Repaired, Cancelled, Unrepairable };

        /// <summary>Statuses that end a ticket's active life — it leaves Pending
        /// Customers and moves to Customer History once it reaches one of these.</summary>
        public static readonly string[] Terminal = { Repaired, Cancelled, Unrepairable };
    }

    /// <summary>One repair part/service checked on a ticket (e.g. "RAM Module", $45.00).</summary>
    public record TicketPart(string Name, decimal Cost);

    /// <summary>Statuses a promotion moves through: Manager drafts/submits it, Admin approves or rejects it.</summary>
    public static class PromotionStatus
    {
        public const string Draft = "Draft";
        public const string PendingApproval = "PendingApproval";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }

    /// <summary>The offer types a Manager can pick when drafting a promotion.</summary>
    public static class PromotionType
    {
        public const string General = "General";
        public const string ThresholdDiscount = "Threshold Discount";
        public const string Referral = "Referral";
        public const string Seasonal = "Seasonal";

        public static readonly string[] All = { General, ThresholdDiscount, Referral, Seasonal };
    }

    /// <summary>One promotion/retention offer a Manager drafted, pending or reviewed by an Admin.</summary>
    public class PromotionRecord
    {
        public int PromotionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = PromotionType.General;
        public string Message { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = PromotionStatus.Draft;
        public string StaffName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }
    }

    /// <summary>One walk-in customer's device + service ticket, kept together since this
    /// app always creates them as a pair (a customer walks in with one device at a time).</summary>
    public class TicketRecord
    {
        public int TicketId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Concern { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
        public string Status { get; set; } = TicketStatus.Diagnose;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public List<TicketPart> SelectedParts { get; set; } = new();

        public string? DiagnosisFindings { get; set; }
        public string ApprovalStatus { get; set; } = "NotRequired";
        public decimal? ApprovedAmount { get; set; }
        public DateTime? ApprovalDecisionAt { get; set; }
        public string? ApprovalMethod { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
        public decimal AmountPaid { get; set; }
        public int? PromotionId { get; set; }
        public string? PromotionTitle { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? DiscountAmount { get; set; }
        public int? FeedbackRating { get; set; }
        public string? FeedbackMessage { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }

        public string FullName =>
            string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    /// <summary>A login account created by an Admin for a Staff or Manager.</summary>
    public class AccountRecord
    {
        public int AccountId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = "Staff"; // "Staff" or "Manager"
        public string Status { get; set; } = "Active";

        public string FullName =>
            string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    /// <summary>A logged notification (email/SMS) sent to a customer about their ticket.</summary>
    public class NotificationRecord
    {
        public int TicketId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
    }

    /// <summary>Subscription states a Superadmin can put a tenant shop into.</summary>
    public static class TenantSubscriptionStatus
    {
        public const string Active = "Active";
        public const string Suspended = "Suspended";

        public static readonly string[] All = { Active, Suspended };
    }

    /// <summary>One repair shop onboarded onto the platform (a tenant), with its owning Admin account.</summary>
    public class TenantRecord
    {
        public int TenantId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string AdminFullName { get; set; } = string.Empty;
        public string AdminUsername { get; set; } = string.Empty;
        public string SubscriptionStatus { get; set; } = TenantSubscriptionStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>One published version of the Terms &amp; Conditions shown to customers.</summary>
    public class TermsVersionRecord
    {
        public int TermsId { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsCurrent { get; set; }
    }

    /// <summary>
    /// In-memory stand-in for the tenant database while the desktop client talks
    /// straight to view-layer controllers instead of the API/EF Core stack. Every
    /// method here is where the real CRMS.infrastructure persistence call belongs
    /// once this app is wired up to a live tenant database.
    /// </summary>
    public static class AppDataStore
    {
        private static int _nextTicketId = 1001;
        private static int _nextAccountId = 1;
        private static int _nextTenantId = 1;
        private static int _nextTermsId = 1;

        private static readonly List<TicketRecord> _pendingTickets = new();
        private static readonly List<TicketRecord> _ticketHistory = new();
        private static readonly List<AccountRecord> _accounts = new();
        private static readonly List<NotificationRecord> _notifications = new();
        private static readonly List<TenantRecord> _tenants = new();
        private static readonly List<TermsVersionRecord> _termsVersions = new();

        public static IReadOnlyList<TicketRecord> PendingTickets => _pendingTickets;
        public static IReadOnlyList<TicketRecord> TicketHistory => _ticketHistory;
        public static IReadOnlyList<AccountRecord> Accounts => _accounts;
        public static IReadOnlyList<NotificationRecord> Notifications => _notifications;
        public static IReadOnlyList<TenantRecord> Tenants => _tenants;
        public static IReadOnlyList<TermsVersionRecord> TermsVersions => _termsVersions;

        public static TicketRecord AddTicket(
            string firstName, string middleName, string lastName,
            string phoneNumber, string email, string deviceName, string concern, decimal estimatedCost)
        {
            var ticket = new TicketRecord
            {
                TicketId = _nextTicketId++,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                Email = email,
                DeviceName = deviceName,
                Concern = concern,
                EstimatedCost = estimatedCost,
                Status = TicketStatus.Diagnose
            };
            _pendingTickets.Add(ticket);
            return ticket;
        }

        /// <summary>
        /// Updates a pending ticket's status and notifies the customer. Once the
        /// status reaches "Repaired" the ticket is considered a finished
        /// transaction and moves out of the pending table into history.
        /// </summary>
        public static void UpdateTicketStatus(int ticketId, string newStatus)
        {
            var ticket = _pendingTickets.FirstOrDefault(t => t.TicketId == ticketId);
            if (ticket is null) return;

            ticket.Status = newStatus;
            NotificationService.NotifyStatusChange(ticket);

            if (newStatus == TicketStatus.Repaired)
            {
                ticket.CompletedAt = DateTime.Now;
                _pendingTickets.Remove(ticket);
                _ticketHistory.Add(ticket);
            }
        }

        internal static void LogNotification(NotificationRecord record) => _notifications.Add(record);

        public static AccountRecord AddAccount(string firstName, string middleName, string lastName, string username, string role)
        {
            var account = new AccountRecord
            {
                AccountId = _nextAccountId++,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Username = username,
                Role = role,
                Status = "Active"
            };
            _accounts.Add(account);
            return account;
        }

        // ===================== Tenants / subscriptions (Superadmin) =====================

        /// <summary>Onboards a new repair shop: its Admin account plus the tenant record Superadmin manages.</summary>
        public static TenantRecord AddTenant(string shopName, string adminFullName, string adminUsername)
        {
            var tenant = new TenantRecord
            {
                TenantId = _nextTenantId++,
                ShopName = shopName,
                AdminFullName = adminFullName,
                AdminUsername = adminUsername,
                SubscriptionStatus = TenantSubscriptionStatus.Active
            };
            _tenants.Add(tenant);
            return tenant;
        }

        public static void UpdateTenantSubscriptionStatus(int tenantId, string newStatus)
        {
            var tenant = _tenants.FirstOrDefault(t => t.TenantId == tenantId);
            if (tenant is null) return;

            tenant.SubscriptionStatus = newStatus;
        }

        // ===================== Terms & Conditions (Superadmin) =====================

        /// <summary>Publishes a new Terms &amp; Conditions version and makes it the current one.</summary>
        public static TermsVersionRecord AddTermsVersion(string version, string content)
        {
            foreach (var existing in _termsVersions)
                existing.IsCurrent = false;

            var record = new TermsVersionRecord
            {
                TermsId = _nextTermsId++,
                Version = version,
                Content = content,
                IsCurrent = true
            };
            _termsVersions.Add(record);
            return record;
        }

        public static void SetCurrentTermsVersion(int termsId)
        {
            foreach (var record in _termsVersions)
                record.IsCurrent = record.TermsId == termsId;
        }
    }
}
