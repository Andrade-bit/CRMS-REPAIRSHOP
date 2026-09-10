using System;

namespace CRMS.winforms.Controllers
{
    /// <summary>
    /// Sends the customer an update whenever their ticket's status changes.
    /// This is a local stand-in — swap SendEmail/SendSms's bodies for a real
    /// SMTP/SendGrid and Twilio (or similar) call when those services are wired up.
    /// </summary>
    public static class NotificationService
    {
        /// <summary>Raised after each notification "send" — used by the UI to show a toast.</summary>
        public static event Action<string>? NotificationSent;

        public static void NotifyStatusChange(TicketRecord ticket)
        {
            string message = ticket.Status switch
            {
                TicketStatus.Diagnose => $"Hi {ticket.FullName}, we've received your {ticket.DeviceName} and started the diagnosis.",
                TicketStatus.Repairing => $"Hi {ticket.FullName}, your {ticket.DeviceName} is now being repaired.",
                TicketStatus.Repaired => $"Hi {ticket.FullName}, your {ticket.DeviceName} has been repaired and is ready for pickup!",
                _ => $"Hi {ticket.FullName}, your {ticket.DeviceName}'s status is now: {ticket.Status}."
            };

            SendEmail(ticket, message);
            SendSms(ticket, message);
        }

        private static void SendEmail(TicketRecord ticket, string message)
        {
            // TODO: replace with a real SMTP/SendGrid call using ticket.Email.
            Log(ticket, "Email", ticket.Email, message);
        }

        private static void SendSms(TicketRecord ticket, string message)
        {
            // TODO: replace with a real Twilio (or similar) call using ticket.PhoneNumber.
            Log(ticket, "SMS", ticket.PhoneNumber, message);
        }

        private static void Log(TicketRecord ticket, string channel, string to, string message)
        {
            AppDataStore.LogNotification(new NotificationRecord
            {
                TicketId = ticket.TicketId,
                CustomerName = ticket.FullName,
                Channel = channel,
                Message = message
            });

            NotificationSent?.Invoke($"{channel} sent to {ticket.FullName} ({to}): \"{message}\"");
        }
    }
}
