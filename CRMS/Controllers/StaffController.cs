using System.Collections.Generic;

namespace CRMS.winforms.Controllers
{
    /// <summary>
    /// Backs StaffMainForm: tracks which sidebar page is active and exposes the
    /// walk-in customer/ticket operations the Staff pages need.
    /// </summary>
    public class StaffController
    {
        private string currentPage;

        public StaffController()
        {
            currentPage = "Customer";
        }

        // Changes the current page
        public string NavigateTo(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName))
            {
                return currentPage;
            }

            currentPage = pageName;

            return currentPage;
        }

        // Gets the current page
        public string GetCurrentPage()
        {
            return currentPage;
        }

        // ===================== Pending customers / tickets =====================

        public IReadOnlyList<TicketRecord> GetPendingTickets() => AppDataStore.PendingTickets;

        public IReadOnlyList<TicketRecord> GetTicketHistory() => AppDataStore.TicketHistory;

        public TicketRecord AddCustomer(
            string firstName, string middleName, string lastName,
            string phoneNumber, string email, string deviceName, string concern, decimal estimatedCost)
        {
            return AppDataStore.AddTicket(
                firstName, middleName, lastName, phoneNumber, email, deviceName, concern, estimatedCost);
        }

        /// <summary>Changes a ticket's status and (via AppDataStore) notifies the customer by email/SMS.</summary>
        public void UpdateTicketStatus(int ticketId, string newStatus)
        {
            AppDataStore.UpdateTicketStatus(ticketId, newStatus);
        }
    }
}
