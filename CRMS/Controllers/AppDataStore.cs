using System;
using System.Collections.Generic;
using System.Linq;

namespace CRMS.winforms.Controllers
{
    /// <summary>Ticket statuses a staff member can move a walk-in repair through.</summary>
    public static class TicketStatus
    {
        public const string Diagnose = "Diagnose";
        public const string Repairing = "Repairing";
        public const string Repaired = "Repaired";

        public static readonly string[] All = { Diagnose, Repairing, Repaired };
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

        public string FullName =>
            string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    /// <summary>A login account created by an Admin for a Staff or Manager.</summary>
    public class AccountRecord
    {
        public int AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = "Staff"; // "Staff" or "Manager"
        public string Status { get; set; } = "Active";
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

        private static readonly List<TicketRecord> _pendingTickets = new();
        private static readonly List<TicketRecord> _ticketHistory = new();
        private static readonly List<AccountRecord> _accounts = new();
        private static readonly List<NotificationRecord> _notifications = new();

        public static IReadOnlyList<TicketRecord> PendingTickets => _pendingTickets;
        public static IReadOnlyList<TicketRecord> TicketHistory => _ticketHistory;
        public static IReadOnlyList<AccountRecord> Accounts => _accounts;
        public static IReadOnlyList<NotificationRecord> Notifications => _notifications;

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

        public static AccountRecord AddAccount(string fullName, string username, string role)
        {
            var account = new AccountRecord
            {
                AccountId = _nextAccountId++,
                FullName = fullName,
                Username = username,
                Role = role,
                Status = "Active"
            };
            _accounts.Add(account);
            return account;
        }
    }
}
