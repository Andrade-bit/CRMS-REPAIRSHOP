using System.Collections.Generic;

namespace CRMS.winforms.Controllers
{
    /// <summary>
    /// Backs AdminMainForm: tracks which sidebar page is active and exposes the
    /// user-management + customer-history operations the Admin page needs.
    /// </summary>
    public class AdminController
    {
        private string currentPage;

        public AdminController()
        {
            currentPage = "Dashboard";
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

        // ===================== User Management =====================

        public IReadOnlyList<AccountRecord> GetAccounts() => AppDataStore.Accounts;

        public AccountRecord CreateAccount(string firstName, string middleName, string lastName, string username, string role)
        {
            return AppDataStore.AddAccount(firstName, middleName, lastName, username, role);
        }

        // ===================== Customer History =====================

        public IReadOnlyList<TicketRecord> GetTicketHistory() => AppDataStore.TicketHistory;
    }
}
