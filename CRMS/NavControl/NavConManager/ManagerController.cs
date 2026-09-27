using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRMS.winforms.Api;

namespace CRMS.winforms.Controllers
{
    /// <summary>
    /// Backs ManagerMainForm: tracks which sidebar page is active and exposes the
    /// ticket work Manager does (same as Staff, including Archive like Admin gets)
    /// plus drafting/submitting Promotions for Admin to review.
    /// </summary>
    public class ManagerController : ITicketWorkController
    {
        private string currentPage;
        private readonly AuthApiClient _apiClient = new();

        /// <summary>The logged-in Manager's JWT, used to call authenticated API endpoints.</summary>
        public string Token { get; }

        /// <summary>The logged-in Manager's own tenant.</summary>
        public int? CompanyId { get; }

        public ManagerController(string token = "", int? companyId = null)
        {
            currentPage = "Pending";
            Token = token;
            CompanyId = companyId;
        }

        /// <summary>This tenant's local-first sync state, for the topbar
        /// indicator — polled periodically, never blocks anything else.</summary>
        public Task<AuthApiClient.SyncStatusDto> GetSyncStatusAsync() => _apiClient.GetSyncStatusAsync(Token);

        /// <summary>This logged-in Manager's own branch, if any — feeds the header's branch pill.</summary>
        public Task<StaffProfileDto> GetMyProfileAsync() => _apiClient.GetMyStaffProfileAsync(Token);

        /// <summary>Which sidebar modules this tenant's subscription plan
        /// unlocks — checked once at shell startup to show/hide nav items.</summary>
        public Task<ModuleAccessDto> GetModuleAccessAsync() => _apiClient.GetModuleAccessAsync(Token);

        public string NavigateTo(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName))
                return currentPage;

            currentPage = pageName;
            return currentPage;
        }

        public string GetCurrentPage() => currentPage;

        /// <summary>The same rich dashboard Admin sees (revenue, ready-for-pickup,
        /// unpaid, ratings, ticket pipeline, staff workload) — automatically
        /// scoped to this Manager's own branch, so a branch-assigned Manager
        /// gets real numbers to base a promotion's Reason on. A Manager with no
        /// branch assigned sees the whole tenant, same as Admin's default.</summary>
        public async Task<DashboardDto?> GetBranchDashboardAsync(DateTime? from = null, DateTime? to = null)
        {
            var profile = await GetMyProfileAsync();
            return await _apiClient.GetDashboardAsync(Token, from, to, profile.BranchId);
        }

        // ===================== Pending customers / tickets =====================

        public async Task<List<TicketRecord>> GetPendingTicketsAsync()
        {
            var tickets = await _apiClient.GetTicketsAsync(Token, "pending");
            return tickets.Select(ToTicketRecord).ToList();
        }

        /// <summary>Tenant-wide counts for the "today" strip on Pending Customers.</summary>
        public Task<TodaySummaryDto> GetTodaySummaryAsync() => _apiClient.GetTodaySummaryAsync(Token);

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

        /// <summary>Approved promotions Manager can offer a walk-in customer.</summary>
        public async Task<List<PromotionDto>> GetApprovedPromotionsAsync()
        {
            var promotions = await _apiClient.GetPromotionsAsync(Token, mine: false);
            return promotions.Where(p => p.Status == "Approved").ToList();
        }

        /// <summary>This tenant's currently published Terms &amp; Conditions
        /// content, shown on the Add Customer screen.</summary>
        public Task<string?> GetCurrentTermsAsync() => _apiClient.GetCurrentTermsAsync(Token);

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
        /// customers don't have their own login, whoever's handling the pickup
        /// enters this on their behalf.</summary>
        public Task<(bool Success, string Message)> SubmitFeedbackAsync(int ticketId, int rating, string? message)
        {
            return _apiClient.SubmitFeedbackAsync(Token, ticketId, rating, message);
        }

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

        /// <summary>Hides a ticket from Customer History (never a real delete) — Manager
        /// gets this too, same as Admin, since Manager oversees Staff's work.</summary>
        public Task<(bool Success, string Message)> ArchiveTicketAsync(int ticketId)
        {
            return _apiClient.ArchiveTicketAsync(Token, ticketId);
        }

        // ===================== Promotions =====================

        /// <summary>Drafts or submits a new promotion for Admin to review. Reason
        /// is the business justification shown to Admin — required once
        /// submitForApproval is true.</summary>
        public Task<(bool Success, string Message, PromotionDto? Promotion)> CreatePromotionAsync(
            string title, string type, string message, string reason, bool submitForApproval)
        {
            return _apiClient.CreatePromotionAsync(Token, title, type, message, reason, submitForApproval);
        }

        /// <summary>This Manager's own submitted promotions, whatever their current status.</summary>
        public async Task<List<PromotionRecord>> GetMyPromotionsAsync()
        {
            var promotions = await _apiClient.GetPromotionsAsync(Token, mine: true);
            return promotions.Select(ToPromotionRecord).ToList();
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

        private static PromotionRecord ToPromotionRecord(PromotionDto dto) => new()
        {
            PromotionId = dto.PromotionId,
            Title = dto.Title,
            Type = dto.Type,
            Message = dto.Message,
            Reason = dto.Reason,
            Status = dto.Status,
            StaffName = dto.StaffName,
            CreatedAt = dto.CreatedAt,
            ReviewedAt = dto.ReviewedAt
        };
    }
}
