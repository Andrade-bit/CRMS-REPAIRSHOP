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

        /// <summary>The logged-in Admin's own display name — used to label who generated a PDF export.</summary>
        public string AdminName { get; }

        public AdminController(string token = "", int? companyId = null, string adminName = "Admin")
        {
            currentPage = "Dashboard";
            Token = token;
            CompanyId = companyId;
            AdminName = adminName;
        }

        /// <summary>Every number on the Dashboard page — a real aggregate over this
        /// tenant's own tickets/promotions, scoped to [from, to] when given
        /// (both default to the current calendar month). Returns null if the
        /// request fails.</summary>
        public Task<DashboardDto?> GetDashboardAsync(DateTime? from = null, DateTime? to = null, int? branchId = null)
        {
            return _apiClient.GetDashboardAsync(Token, from, to, branchId);
        }

        /// <summary>Resolved-ticket revenue per month for the Reports page's
        /// Monthly Sales chart.</summary>
        public Task<List<MonthlySalesEntryDto>> GetMonthlySalesAsync(int months = 12)
        {
            return _apiClient.GetMonthlySalesAsync(Token, months);
        }

        /// <summary>The individual tickets behind one month's revenue total, for
        /// the Monthly Sales drill-down.</summary>
        public Task<MonthlySalesDetailDto?> GetMonthlySalesDetailAsync(int year, int month)
        {
            return _apiClient.GetMonthlySalesDetailAsync(Token, year, month);
        }

        /// <summary>Customers ranked by total spend / repeat-visit count.</summary>
        public Task<List<TopCustomerEntryDto>> GetTopCustomersAsync(int take = 15)
        {
            return _apiClient.GetTopCustomersAsync(Token, take);
        }

        /// <summary>Most common device types ("devices") or stated repair
        /// concerns ("concerns") across resolved tickets.</summary>
        public Task<List<CommonValueEntryDto>> GetCommonValuesAsync(string kind, int take = 10)
        {
            return _apiClient.GetCommonValuesAsync(Token, kind, take);
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
        public Task<List<UserAccount>> GetAccountsAsync() => _apiClient.GetAdminAccountsAsync(Token);

        /// <summary>Creates a real Staff/Manager account, tagged to this Admin's own
        /// tenant. The server rejects anything other than Staff/Manager. branchId
        /// is optional — null means this account belongs to the tenant directly,
        /// not any specific branch.</summary>
        public Task<(bool Success, string Message)> CreateAccountAsync(
            string firstName, string middleName, string lastName, string username, string password, string role,
            int? branchId = null)
        {
            string fullName = $"{firstName} {middleName} {lastName}".Replace("  ", " ").Trim();
            return _apiClient.CreateAdminAccountAsync(Token, username, password, fullName, role, branchId);
        }

        /// <summary>Sets a new password for an existing Staff/Manager account in this
        /// Admin's own tenant.</summary>
        public Task<(bool Success, string Message)> ResetPasswordAsync(string username, string newPassword) =>
            _apiClient.ResetAdminAccountPasswordAsync(Token, username, newPassword);

        /// <summary>Suspends or reactivates a Staff/Manager account — a suspended
        /// account keeps its data but can no longer log in.</summary>
        public Task<(bool Success, string Message)> SetAccountStatusAsync(string username, string status) =>
            _apiClient.SetAccountStatusAsync(Token, username, status);

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
            }).ToList();
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

        /// <summary>Every piece of feedback recorded across this tenant, for the Reports page.</summary>
        public Task<List<FeedbackEntryDto>> GetAllFeedbackAsync()
        {
            return _apiClient.GetAllFeedbackAsync(Token);
        }

        /// <summary>Resolved-ticket value split by whether a promotion was applied.</summary>
        public Task<PromotionComparisonResponseDto?> GetPromotionComparisonAsync()
        {
            return _apiClient.GetPromotionComparisonAsync(Token);
        }

        /// <summary>Every Approved promotion's real usage/discount/revenue track record.</summary>
        public Task<List<PromotionEffectivenessEntryDto>> GetPromotionEffectivenessAsync()
        {
            return _apiClient.GetPromotionEffectivenessAsync(Token);
        }

        /// <summary>Hides a ticket from Customer History (never a real delete) — Admin-only.</summary>
        public Task<(bool Success, string Message)> ArchiveTicketAsync(int ticketId)
        {
            return _apiClient.ArchiveTicketAsync(Token, ticketId);
        }

        // ===================== Sync status =====================

        /// <summary>This tenant's local-first sync state, for the topbar
        /// indicator — polled periodically, never blocks anything else.</summary>
        public Task<AuthApiClient.SyncStatusDto> GetSyncStatusAsync() => _apiClient.GetSyncStatusAsync(Token);

        /// <summary>Which sidebar modules this tenant's subscription plan
        /// unlocks — checked once at shell startup to show/hide nav items.</summary>
        public Task<ModuleAccessDto> GetModuleAccessAsync() => _apiClient.GetModuleAccessAsync(Token);

        // ===================== Branch =====================

        /// <summary>Every branch this tenant has, or null if this tenant has no
        /// Branch Management at all — the Branch Management nav item only shows
        /// up when this returns non-null (even an empty list still shows it, so
        /// Admin can add the first branch).</summary>
        public Task<List<BranchDto>?> GetBranchesAsync() => _apiClient.GetBranchesAsync(Token);

        /// <summary>Adds a new branch to this tenant.</summary>
        public Task<(bool Success, string Message, BranchDto? Branch)> CreateBranchAsync(string name, string address, string phone, string email) =>
            _apiClient.CreateBranchAsync(Token, name, address, phone, email);

        /// <summary>Updates one of this tenant's branches.</summary>
        public Task<(bool Success, string Message)> UpdateBranchAsync(int branchId, string name, string address, string phone, string email) =>
            _apiClient.UpdateBranchAsync(Token, branchId, name, address, phone, email);

        /// <summary>Hides a branch from Branch Management and the branch picker — never a real delete.</summary>
        public Task<(bool Success, string Message)> ArchiveBranchAsync(int branchId) =>
            _apiClient.ArchiveBranchAsync(Token, branchId);

        // ===================== Promotions =====================

        /// <summary>Every promotion submitted across this tenant, by any Manager, whatever its status.</summary>
        public async Task<List<PromotionRecord>> GetAllPromotionsAsync()
        {
            var promotions = await _apiClient.GetPromotionsAsync(Token, mine: false);
            return promotions.Select(dto => new PromotionRecord
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
            }).ToList();
        }

        /// <summary>Approves a promotion a Manager submitted, letting it move forward.</summary>
        public Task<(bool Success, string Message)> ApprovePromotionAsync(int promotionId) =>
            _apiClient.ReviewPromotionAsync(Token, promotionId, approve: true);

        /// <summary>Rejects a promotion a Manager submitted.</summary>
        public Task<(bool Success, string Message)> RejectPromotionAsync(int promotionId) =>
            _apiClient.ReviewPromotionAsync(Token, promotionId, approve: false);

        /// <summary>Hides a promotion from the review list — never a real delete.</summary>
        public Task<(bool Success, string Message)> ArchivePromotionAsync(int promotionId) =>
            _apiClient.ArchivePromotionAsync(Token, promotionId);

        // ===================== Terms & Conditions =====================

        /// <summary>Every Terms &amp; Conditions version this tenant has published,
        /// newest first.</summary>
        public Task<List<TenantTermsVersionDto>> GetTermsVersionsAsync() =>
            _apiClient.GetTenantTermsVersionsAsync(Token);

        /// <summary>Publishes a new Terms &amp; Conditions version and makes it
        /// current — Staff will see it on the Add Customer screen right away.</summary>
        public Task<(bool Success, string Message)> PublishTermsVersionAsync(string version, string content) =>
            _apiClient.PublishTenantTermsVersionAsync(Token, version, content);

        /// <summary>Makes an already-published version current again, without
        /// republishing it as a new one — e.g. rolling back after a bad edit.</summary>
        public Task<(bool Success, string Message)> SetCurrentTermsVersionAsync(int termsId) =>
            _apiClient.SetCurrentTermsVersionAsync(Token, termsId);
    }
}
