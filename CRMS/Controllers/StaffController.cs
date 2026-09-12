using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRMS.winforms.Api;

namespace CRMS.winforms.Controllers
{
    /// <summary>
    /// Backs StaffMainForm: tracks which sidebar page is active and exposes the
    /// walk-in customer/ticket operations the Staff pages need, backed by the
    /// real per-tenant database via CRMS.api's /api/tickets endpoints.
    /// </summary>
    public class StaffController
    {
        private string currentPage;
        private readonly AuthApiClient _apiClient = new();

        /// <summary>The logged-in Staff's JWT, used to call authenticated API endpoints.</summary>
        public string Token { get; }

        /// <summary>The logged-in Staff's own tenant.</summary>
        public int? CompanyId { get; }

        public StaffController(string token = "", int? companyId = null)
        {
            currentPage = "Customer";
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

        // ===================== Pending customers / tickets =====================

        public async Task<List<TicketRecord>> GetPendingTicketsAsync()
        {
            var tickets = await _apiClient.GetTicketsAsync(Token, "pending");
            return tickets.Select(ToTicketRecord).ToList();
        }

        public async Task<List<TicketRecord>> GetTicketHistoryAsync()
        {
            var tickets = await _apiClient.GetTicketsAsync(Token, "history");
            return tickets.Select(ToTicketRecord).ToList();
        }

        public Task<(bool Success, string Message, TicketDto? Ticket)> AddCustomerAsync(
            string firstName, string middleName, string lastName,
            string phoneNumber, string email, string deviceName, string concern, decimal estimatedCost)
        {
            return _apiClient.CreateTicketAsync(
                Token, firstName, middleName, lastName, phoneNumber, email, deviceName, concern, estimatedCost);
        }

        /// <summary>Changes a ticket's status; the API notifies the customer and stamps CompletedAt when Repaired.</summary>
        public Task<(bool Success, string Message)> UpdateTicketStatusAsync(int ticketId, string newStatus)
        {
            return _apiClient.UpdateTicketStatusAsync(Token, ticketId, newStatus);
        }

        /// <summary>Marks whether the customer has claimed their device yet.</summary>
        public Task<(bool Success, string Message)> UpdatePickupStatusAsync(int ticketId, bool pickedUp)
        {
            return _apiClient.UpdateTicketPickupStatusAsync(Token, ticketId, pickedUp);
        }

        private static TicketRecord ToTicketRecord(TicketDto dto) => new()
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
            PickedUpAt = dto.PickedUpAt,
            StaffName = dto.StaffName
        };
    }
}
