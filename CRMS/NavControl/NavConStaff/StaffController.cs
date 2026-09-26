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
    public class StaffController : ITicketWorkController
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

        /// <summary>This tenant's local-first sync state, for the topbar
        /// indicator — polled periodically, never blocks anything else.</summary>
        public Task<AuthApiClient.SyncStatusDto> GetSyncStatusAsync() => _apiClient.GetSyncStatusAsync(Token);

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

        public Task<List<CustomerSearchResultDto>> SearchCustomersAsync(string query)
        {
            return _apiClient.SearchCustomersAsync(Token, query);
        }

        public Task<(bool Success, string Message, TicketDto? Ticket)> AddCustomerAsync(
            string firstName, string middleName, string lastName,
            string phoneNumber, string email, string deviceName, string concern, decimal estimatedCost,
            IEnumerable<TicketPart>? selectedParts = null, int? existingCustomerId = null, int? promotionId = null,
            decimal? originalPrice = null)
        {
            return _apiClient.CreateTicketAsync(
                Token, firstName, middleName, lastName, phoneNumber, email, deviceName, concern, estimatedCost,
                selectedParts?.Select(p => new SelectedPartDto { Name = p.Name, Cost = p.Cost }), existingCustomerId, promotionId,
                originalPrice);
        }

        /// <summary>Approved promotions Staff can offer a walk-in customer.</summary>
        public async Task<List<PromotionDto>> GetApprovedPromotionsAsync()
        {
            var promotions = await _apiClient.GetPromotionsAsync(Token, mine: false);
            return promotions.Where(p => p.Status == "Approved").ToList();
        }

        /// <summary>This tenant's currently published Terms &amp; Conditions
        /// content, shown on the Add Customer screen.</summary>
        public Task<string?> GetCurrentTermsAsync() => _apiClient.GetCurrentTermsAsync(Token);

        /// <summary>Changes a ticket's status; the API notifies the customer and stamps CompletedAt when Repaired.</summary>
        public Task<(bool Success, string Message)> UpdateTicketStatusAsync(int ticketId, string newStatus)
        {
            return _apiClient.UpdateTicketStatusAsync(Token, ticketId, newStatus);
        }

        public Task<(bool Success, string Message)> RecordApprovalAsync(
            int ticketId, string diagnosisFindings, decimal estimatedCost, string decision,
            decimal? approvedAmount = null, string? method = null)
        {
            return _apiClient.RecordApprovalAsync(Token, ticketId, diagnosisFindings, estimatedCost, decision, approvedAmount, method);
        }

        /// <summary>Marks whether the customer has claimed their device yet.</summary>
        public Task<(bool Success, string Message)> UpdatePickupStatusAsync(int ticketId, bool pickedUp)
        {
            return _apiClient.UpdateTicketPickupStatusAsync(Token, ticketId, pickedUp);
        }

        public Task<(bool Success, string Message)> UpdatePaymentStatusAsync(int ticketId, string paymentStatus, decimal? amountPaid = null)
        {
            return _apiClient.UpdateTicketPaymentStatusAsync(Token, ticketId, paymentStatus, amountPaid);
        }

        /// <summary>Records the customer's rating/comment for a ticket — since
        /// customers don't have their own login, Staff enters this on their
        /// behalf, typically right at pickup.</summary>
        public Task<(bool Success, string Message)> SubmitFeedbackAsync(int ticketId, int rating, string? message)
        {
            return _apiClient.SubmitFeedbackAsync(Token, ticketId, rating, message);
        }

        /// <summary>Fixes the customer's name/phone/email, the device name/concern/
        /// estimated cost, and which repair parts are checked, on an existing ticket.</summary>
        public Task<(bool Success, string Message)> UpdateTicketDetailsAsync(
            int ticketId, string fullName, string phone, string email, string deviceName, string concern, decimal estimatedCost,
            IEnumerable<TicketPart>? selectedParts = null, int? promotionId = null, bool clearPromotion = false,
            decimal? originalPrice = null)
        {
            return _apiClient.UpdateTicketDetailsAsync(
                Token, ticketId, fullName, phone, email, deviceName, concern, estimatedCost,
                selectedParts?.Select(p => new SelectedPartDto { Name = p.Name, Cost = p.Cost }), promotionId, clearPromotion,
                originalPrice);
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
            ArchivedAt = dto.ArchivedAt,
            StaffName = dto.StaffName,
            SelectedParts = dto.SelectedParts.Select(p => new TicketPart(p.Name, p.Cost)).ToList(),
            DiagnosisFindings = dto.DiagnosisFindings,
            ApprovalStatus = dto.ApprovalStatus,
            ApprovedAmount = dto.ApprovedAmount,
            ApprovalDecisionAt = dto.ApprovalDecisionAt,
            ApprovalMethod = dto.ApprovalMethod,
            PaymentStatus = dto.PaymentStatus,
            AmountPaid = dto.AmountPaid,
            PromotionId = dto.PromotionId,
            PromotionTitle = dto.PromotionTitle,
            OriginalPrice = dto.OriginalPrice,
            DiscountAmount = dto.DiscountAmount,
            FeedbackRating = dto.FeedbackRating,
            FeedbackMessage = dto.FeedbackMessage,
            BranchId = dto.BranchId,
            BranchName = dto.BranchName
        };
    }
}
