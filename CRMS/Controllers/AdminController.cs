using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRMS.winforms.Api;

namespace CRMS.winforms.Controllers
{
    /// <summary>
    /// Backs AdminMainForm: tracks which sidebar page is active and exposes the
    /// user-management + customer-history operations the Admin page needs.
    /// </summary>
    public class AdminController
    {
        private string currentPage;
        private readonly AuthApiClient _apiClient = new();

        /// <summary>The logged-in Admin's JWT, used to call authenticated API endpoints (e.g. creating accounts).</summary>
        public string Token { get; }

        /// <summary>The logged-in Admin's own tenant — new Staff/Manager accounts they create belong to this same CompanyId.</summary>
        public int? CompanyId { get; }

        public AdminController(string token = "", int? companyId = null)
        {
            currentPage = "Dashboard";
            Token = token;
            CompanyId = companyId;
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

        /// <summary>Fetches the real account list for this Admin's own tenant.</summary>
        public Task<List<UserAccount>> GetAccountsAsync() => _apiClient.GetUsersAsync(CompanyId);

        /// <summary>Creates a real Staff/Manager account, tagged to this Admin's own tenant.</summary>
        public Task<(bool Success, string Message)> CreateAccountAsync(
            string firstName, string middleName, string lastName, string username, string password, string role)
        {
            string fullName = $"{firstName} {middleName} {lastName}".Replace("  ", " ").Trim();
            return _apiClient.RegisterAsync(Token, username, password, fullName, role, CompanyId);
        }

        /// <summary>Sets a new password for an existing Staff/Manager account.</summary>
        public Task<(bool Success, string Message)> ResetPasswordAsync(string username, string newPassword) =>
            _apiClient.ResetPasswordAsync(username, newPassword);

        // ===================== Customer History =====================

        /// <summary>Fetches real completed tickets across this Admin's whole tenant (every Staff's work).</summary>
        public async Task<List<TicketRecord>> GetTicketHistoryAsync()
        {
            var tickets = await _apiClient.GetTicketsAsync(Token, "history");
            return tickets.Select(dto => new TicketRecord
            {
                TicketId = dto.TicketId,
                FirstName = dto.CustomerName,
                PhoneNumber = dto.Phone,
                Email = dto.Email,
                DeviceName = dto.DeviceName,
                Concern = dto.Concern,
                EstimatedCost = dto.EstimatedCost,
                Status = dto.Status,
                CreatedAt = dto.CreatedAt,
                CompletedAt = dto.CompletedAt,
                StaffName = dto.StaffName
            }).ToList();
        }
    }
}
