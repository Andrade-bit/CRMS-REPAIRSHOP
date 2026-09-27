using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CRMS.winforms.Api
{
    public class LoginResult
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; }

        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; } = new List<string>();

        [JsonPropertyName("companyId")]
        public int? CompanyId { get; set; }
    }

    public class UserAccount
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class SelectedPartDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("cost")]
        public decimal Cost { get; set; }
    }

    public class RevenuePointDto
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }

    public class PaymentBreakdownEntryDto
    {
        [JsonPropertyName("paymentStatus")]
        public string PaymentStatus { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("ticketCount")]
        public int TicketCount { get; set; }
    }

    public class PipelineEntryDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class StaffWorkloadEntryDto
    {
        [JsonPropertyName("staffName")]
        public string StaffName { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class TicketSummaryEntryDto
    {
        [JsonPropertyName("ticketId")]
        public int TicketId { get; set; }

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class PendingPromotionEntryDto
    {
        [JsonPropertyName("promotionId")]
        public int PromotionId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("staffName")]
        public string StaffName { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }


    public class DashboardDto
    {
        [JsonPropertyName("revenueMtd")]
        public decimal RevenueMtd { get; set; }

        [JsonPropertyName("readyNotPickedUp")]
        public int ReadyNotPickedUp { get; set; }

        [JsonPropertyName("unpaidTicketsCount")]
        public int UnpaidTicketsCount { get; set; }

        [JsonPropertyName("promotionsPendingCount")]
        public int PromotionsPendingCount { get; set; }

        [JsonPropertyName("revenueTrend")]
        public List<RevenuePointDto> RevenueTrend { get; set; } = new();

        [JsonPropertyName("paymentBreakdown")]
        public List<PaymentBreakdownEntryDto> PaymentBreakdown { get; set; } = new();

        [JsonPropertyName("ticketPipeline")]
        public List<PipelineEntryDto> TicketPipeline { get; set; } = new();

        [JsonPropertyName("staffWorkload")]
        public List<StaffWorkloadEntryDto> StaffWorkload { get; set; } = new();

        [JsonPropertyName("revenueMtdTickets")]
        public List<TicketSummaryEntryDto> RevenueMtdTickets { get; set; } = new();

        [JsonPropertyName("readyNotPickedUpTickets")]
        public List<TicketSummaryEntryDto> ReadyNotPickedUpTickets { get; set; } = new();

        [JsonPropertyName("unpaidTickets")]
        public List<TicketSummaryEntryDto> UnpaidTickets { get; set; } = new();

        [JsonPropertyName("pendingPromotions")]
        public List<PendingPromotionEntryDto> PendingPromotions { get; set; } = new();

        [JsonPropertyName("averageRating")]
        public double AverageRating { get; set; }

        [JsonPropertyName("ratingCount")]
        public int RatingCount { get; set; }

        [JsonPropertyName("ratingDistribution")]
        public List<RatingBucketEntryDto> RatingDistribution { get; set; } = new();

        [JsonPropertyName("recentFeedbackMessage")]
        public string? RecentFeedbackMessage { get; set; }

        [JsonPropertyName("recentFeedbackCustomer")]
        public string? RecentFeedbackCustomer { get; set; }
    }

    public class RatingBucketEntryDto
    {
        [JsonPropertyName("stars")]
        public int Stars { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class MonthlySalesEntryDto
    {
        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("revenue")]
        public decimal Revenue { get; set; }

        [JsonPropertyName("ticketCount")]
        public int TicketCount { get; set; }
    }

    public class MonthlySalesDetailDto
    {
        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("revenue")]
        public decimal Revenue { get; set; }

        [JsonPropertyName("tickets")]
        public List<TicketSummaryEntryDto> Tickets { get; set; } = new();
    }

    public class TopCustomerEntryDto
    {
        [JsonPropertyName("customerId")]
        public int CustomerId { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("visitCount")]
        public int VisitCount { get; set; }

        [JsonPropertyName("totalSpent")]
        public decimal TotalSpent { get; set; }

        [JsonPropertyName("lastVisit")]
        public DateTime LastVisit { get; set; }
    }

    public class CommonValueEntryDto
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class PromotionComparisonSideDto
    {
        [JsonPropertyName("ticketCount")]
        public int TicketCount { get; set; }

        [JsonPropertyName("avgTicketValue")]
        public decimal AvgTicketValue { get; set; }

        [JsonPropertyName("totalRevenue")]
        public decimal TotalRevenue { get; set; }

        [JsonPropertyName("totalDiscount")]
        public decimal TotalDiscount { get; set; }
    }

    public class PromotionComparisonResponseDto
    {
        [JsonPropertyName("withPromotion")]
        public PromotionComparisonSideDto WithPromotion { get; set; } = new();

        [JsonPropertyName("withoutPromotion")]
        public PromotionComparisonSideDto WithoutPromotion { get; set; } = new();
    }

    public class PromotionEffectivenessEntryDto
    {
        [JsonPropertyName("promotionId")]
        public int PromotionId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("timesUsed")]
        public int TimesUsed { get; set; }

        [JsonPropertyName("discountGiven")]
        public decimal DiscountGiven { get; set; }

        [JsonPropertyName("revenueGenerated")]
        public decimal RevenueGenerated { get; set; }

        [JsonPropertyName("avgTicketValue")]
        public decimal AvgTicketValue { get; set; }
    }

    public class FeedbackEntryDto
    {
        [JsonPropertyName("feedbackId")]
        public int FeedbackId { get; set; }

        [JsonPropertyName("ticketId")]
        public int TicketId { get; set; }

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; } = string.Empty;

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("handledBy")]
        public string HandledBy { get; set; } = string.Empty;

        [JsonPropertyName("ticketCreatedAt")]
        public DateTime TicketCreatedAt { get; set; }
    }

    public class CustomerSearchResultDto
    {
        [JsonPropertyName("customerId")]
        public int CustomerId { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("ticketCount")]
        public int TicketCount { get; set; }
    }

    public class PromotionDto
    {
        [JsonPropertyName("promotionId")]
        public int PromotionId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("staffName")]
        public string StaffName { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("reviewedAt")]
        public DateTime? ReviewedAt { get; set; }
    }

    public class TicketLookupDto
    {
        [JsonPropertyName("ticketId")]
        public int TicketId { get; set; }

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

    public class TodaySummaryDto
    {
        [JsonPropertyName("ticketsToday")]
        public int TicketsToday { get; set; }

        [JsonPropertyName("awaitingPickup")]
        public int AwaitingPickup { get; set; }

        [JsonPropertyName("needsApproval")]
        public int NeedsApproval { get; set; }
    }

    public class StaffProfileDto
    {
        [JsonPropertyName("branchId")]
        public int? BranchId { get; set; }

        [JsonPropertyName("branchName")]
        public string? BranchName { get; set; }
    }

    public class TicketDto
    {
        [JsonPropertyName("ticketId")]
        public int TicketId { get; set; }

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; } = string.Empty;

        [JsonPropertyName("concern")]
        public string Concern { get; set; } = string.Empty;

        [JsonPropertyName("estimatedCost")]
        public decimal EstimatedCost { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("pickedUpAt")]
        public DateTime? PickedUpAt { get; set; }

        [JsonPropertyName("archivedAt")]
        public DateTime? ArchivedAt { get; set; }

        [JsonPropertyName("staffName")]
        public string StaffName { get; set; } = string.Empty;

        [JsonPropertyName("selectedParts")]
        public List<SelectedPartDto> SelectedParts { get; set; } = new List<SelectedPartDto>();

        [JsonPropertyName("diagnosisFindings")]
        public string? DiagnosisFindings { get; set; }

        [JsonPropertyName("approvalStatus")]
        public string ApprovalStatus { get; set; } = "NotRequired";

        [JsonPropertyName("approvedAmount")]
        public decimal? ApprovedAmount { get; set; }

        [JsonPropertyName("approvalDecisionAt")]
        public DateTime? ApprovalDecisionAt { get; set; }

        [JsonPropertyName("approvalMethod")]
        public string? ApprovalMethod { get; set; }

        [JsonPropertyName("paymentStatus")]
        public string PaymentStatus { get; set; } = "Unpaid";

        [JsonPropertyName("amountPaid")]
        public decimal AmountPaid { get; set; }

        [JsonPropertyName("promotionId")]
        public int? PromotionId { get; set; }

        [JsonPropertyName("promotionTitle")]
        public string? PromotionTitle { get; set; }

        [JsonPropertyName("originalPrice")]
        public decimal? OriginalPrice { get; set; }

        [JsonPropertyName("discountAmount")]
        public decimal? DiscountAmount { get; set; }

        [JsonPropertyName("feedbackRating")]
        public int? FeedbackRating { get; set; }

        [JsonPropertyName("feedbackMessage")]
        public string? FeedbackMessage { get; set; }

        [JsonPropertyName("branchId")]
        public int? BranchId { get; set; }

        [JsonPropertyName("branchName")]
        public string? BranchName { get; set; }
    }

    // ===================== Superadmin =====================

    public class TenantSubscriptionEntryDto
    {
        [JsonPropertyName("companyId")]
        public int CompanyId { get; set; }

        [JsonPropertyName("companyCode")]
        public string CompanyCode { get; set; } = string.Empty;

        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; } = string.Empty;

        [JsonPropertyName("adminUsername")]
        public string AdminUsername { get; set; } = string.Empty;

        [JsonPropertyName("adminName")]
        public string AdminName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("billingCycle")]
        public string BillingCycle { get; set; } = string.Empty;

        [JsonPropertyName("nextBillingDate")]
        public DateTime? NextBillingDate { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("plan")]
        public string Plan { get; set; } = "AllAccess";

        [JsonPropertyName("companyCreatedAt")]
        public DateTime CompanyCreatedAt { get; set; }
    }

    /// <summary>Which sidebar modules the caller's own tenant currently has,
    /// per its subscription plan — see CRMS.api's ModuleAccessController.</summary>
    public class ModuleAccessDto
    {
        [JsonPropertyName("promotions")]
        public bool Promotions { get; set; } = true;

        [JsonPropertyName("reports")]
        public bool Reports { get; set; } = true;

        [JsonPropertyName("branches")]
        public bool Branches { get; set; } = true;

        [JsonPropertyName("pdfExport")]
        public bool PdfExport { get; set; } = true;

        [JsonPropertyName("emailNotifications")]
        public bool EmailNotifications { get; set; } = true;

        [JsonPropertyName("offlineSync")]
        public bool OfflineSync { get; set; } = true;
    }

    public class TermsVersionEntryDto
    {
        [JsonPropertyName("termsId")]
        public int TermsId { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("isCurrent")]
        public bool IsCurrent { get; set; }

        [JsonPropertyName("planScope")]
        public string PlanScope { get; set; } = "All";
    }

    /// <summary>A tenant's own Terms &amp; Conditions version — separate from
    /// TermsVersionEntryDto, which is Superadmin's platform-wide version.</summary>
    public class TenantTermsVersionDto
    {
        [JsonPropertyName("termsId")]
        public int TermsId { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("isCurrent")]
        public bool IsCurrent { get; set; }
    }

    public class CurrentTermsDto
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    public class SuperadminAdminAccountEntryDto
    {
        [JsonPropertyName("companyId")]
        public int CompanyId { get; set; }

        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class TenantPlatformSummaryDto
    {
        [JsonPropertyName("companyId")]
        public int CompanyId { get; set; }

        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; } = string.Empty;

        [JsonPropertyName("ticketsResolved")]
        public int TicketsResolved { get; set; }

        [JsonPropertyName("revenue")]
        public decimal Revenue { get; set; }
    }

    public class PlatformReportResponseDto
    {
        [JsonPropertyName("ticketsResolved30d")]
        public int TicketsResolved30d { get; set; }

        [JsonPropertyName("revenue30d")]
        public decimal Revenue30d { get; set; }

        [JsonPropertyName("activeStaffAccounts")]
        public int ActiveStaffAccounts { get; set; }

        [JsonPropertyName("averageRating")]
        public double AverageRating { get; set; }

        [JsonPropertyName("ratingCount")]
        public int RatingCount { get; set; }

        [JsonPropertyName("byTenant")]
        public List<TenantPlatformSummaryDto> ByTenant { get; set; } = new();
    }

    public class BranchDto
    {
        [JsonPropertyName("branchId")]
        public int BranchId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    public class AuthApiClient
    {
        // Same address used in CRMS.api.http. Move this to a config file later.
        private const string BaseUrl = "http://localhost:5029";

        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>A failed request's body is JSON — either a plain string
        /// (BadRequest("...")) or an array of strings (ASP.NET Identity's
        /// result.Errors) — either of which shown raw would put literal quotes
        /// or brackets in front of the user (e.g. ["Username 'x' is already
        /// taken."]). This unwraps either shape into one readable sentence,
        /// falling back to the raw body if it isn't JSON at all.</summary>
        private static string CleanErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return body;

            try
            {
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.ValueKind switch
                {
                    JsonValueKind.String => doc.RootElement.GetString() ?? body,
                    JsonValueKind.Array => string.Join(" ", doc.RootElement.EnumerateArray()
                        .Where(el => el.ValueKind == JsonValueKind.String)
                        .Select(el => el.GetString())),
                    _ => body
                };
            }
            catch (JsonException)
            {
                return body;
            }
        }

        /// <summary>
        /// Calls POST /api/auth/login. Returns null if the credentials were
        /// rejected (401/400) or the request failed for any reason.
        /// </summary>
        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            var payload = new { username, password };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync("/api/auth/login", content);

                if (!response.IsSuccessStatusCode)
                    return null; // wrong credentials, or server-side validation error

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<LoginResult>(body, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                // API not running / unreachable
                return null;
            }
        }

        /// <summary>Calls GET /api/admin/accounts — every account in the caller's
        /// own tenant, resolved from their token (never a client-supplied
        /// companyId). Requires an authenticated Admin. Returns an empty list if
        /// the request fails for any reason.</summary>
        public async Task<List<UserAccount>> GetAdminAccountsAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/accounts");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<UserAccount>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<UserAccount>>(body, _jsonOptions) ?? new List<UserAccount>();
            }
            catch (HttpRequestException)
            {
                return new List<UserAccount>();
            }
        }

        /// <summary>Calls POST /api/admin/accounts — creates a Staff or Manager
        /// account in the caller's own tenant (server rejects any other role).
        /// Returns (true, serverMessage) on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> CreateAdminAccountAsync(
            string token, string username, string password, string fullName, string role, int? branchId = null)
        {
            var payload = new { username, password, fullName, role, branchId };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/accounts") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, body.Trim('"'));

                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/admin/accounts/{username}/reset-password — only
        /// works if that account belongs to the caller's own tenant. Returns
        /// (true, serverMessage) on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> ResetAdminAccountPasswordAsync(string token, string username, string newPassword)
        {
            var payload = new { newPassword };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/accounts/{Uri.EscapeDataString(username)}/reset-password") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, body.Trim('"'));

                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/admin/accounts/{username}/status — suspends or
        /// reactivates a Staff/Manager account in the caller's own tenant. status
        /// must be "Active" or "Suspended". Returns (true, serverMessage) on
        /// success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> SetAccountStatusAsync(string token, string username, string status)
        {
            var payload = new { status };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/accounts/{Uri.EscapeDataString(username)}/status") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, body.Trim('"'));

                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls GET /api/dashboard — every number is a real aggregate over
        /// the caller's own tenant data, scoped to [from, to] (inclusive of both
        /// days) when given. Omitting both defaults to the current calendar month,
        /// matching the Dashboard's default "This Month" filter. Returns null if
        /// the request fails.</summary>
        public async Task<DashboardDto?> GetDashboardAsync(string token, DateTime? from = null, DateTime? to = null, int? branchId = null)
        {
            string url = "/api/dashboard";
            if (from.HasValue || to.HasValue || branchId.HasValue)
            {
                var query = new List<string>();
                if (from.HasValue) query.Add($"from={from.Value:yyyy-MM-dd}");
                if (to.HasValue) query.Add($"to={to.Value:yyyy-MM-dd}");
                if (branchId.HasValue) query.Add($"branchId={branchId.Value}");
                url += "?" + string.Join("&", query);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<DashboardDto>(body, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        /// <summary>Calls GET /api/reports/monthly-sales — resolved-ticket revenue
        /// per month for the last `months` months (default 12). Returns an empty
        /// list if the request fails.</summary>
        public async Task<List<MonthlySalesEntryDto>> GetMonthlySalesAsync(string token, int months = 12)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/reports/monthly-sales?months={months}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<MonthlySalesEntryDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<MonthlySalesEntryDto>>(body, _jsonOptions) ?? new List<MonthlySalesEntryDto>();
            }
            catch (HttpRequestException)
            {
                return new List<MonthlySalesEntryDto>();
            }
        }

        /// <summary>Calls GET /api/reports/monthly-sales/{year}/{month} — the
        /// individual tickets behind one month's total, for the Reports page's
        /// drill-down. Returns null if the request fails.</summary>
        public async Task<MonthlySalesDetailDto?> GetMonthlySalesDetailAsync(string token, int year, int month)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/reports/monthly-sales/{year}/{month}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MonthlySalesDetailDto>(body, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        /// <summary>Calls GET /api/reports/top-customers — customers ranked by
        /// total spend and repeat-visit count. Returns an empty list if the
        /// request fails.</summary>
        public async Task<List<TopCustomerEntryDto>> GetTopCustomersAsync(string token, int take = 15)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/reports/top-customers?take={take}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<TopCustomerEntryDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TopCustomerEntryDto>>(body, _jsonOptions) ?? new List<TopCustomerEntryDto>();
            }
            catch (HttpRequestException)
            {
                return new List<TopCustomerEntryDto>();
            }
        }

        /// <summary>Calls GET /api/reports/common-devices or /common-concerns —
        /// which values (device type / stated concern) show up most often across
        /// resolved tickets. Returns an empty list if the request fails.</summary>
        public async Task<List<CommonValueEntryDto>> GetCommonValuesAsync(string token, string kind, int take = 10)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/reports/common-{kind}?take={take}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<CommonValueEntryDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<CommonValueEntryDto>>(body, _jsonOptions) ?? new List<CommonValueEntryDto>();
            }
            catch (HttpRequestException)
            {
                return new List<CommonValueEntryDto>();
            }
        }

        /// <summary>Calls GET /api/reports/promotion-comparison — resolved-ticket
        /// value split by whether a promotion was applied. Returns null if the
        /// request fails.</summary>
        public async Task<PromotionComparisonResponseDto?> GetPromotionComparisonAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/reports/promotion-comparison");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PromotionComparisonResponseDto>(body, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        /// <summary>Calls GET /api/reports/promotion-effectiveness — every
        /// Approved promotion's real usage/discount/revenue track record.
        /// Returns an empty list if the request fails.</summary>
        public async Task<List<PromotionEffectivenessEntryDto>> GetPromotionEffectivenessAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/reports/promotion-effectiveness");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<PromotionEffectivenessEntryDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<PromotionEffectivenessEntryDto>>(body, _jsonOptions) ?? new List<PromotionEffectivenessEntryDto>();
            }
            catch (HttpRequestException)
            {
                return new List<PromotionEffectivenessEntryDto>();
            }
        }

        /// <summary>Calls PUT /api/tickets/{id}/feedback — records (or updates,
        /// if one already exists) the customer's rating/comment for that ticket.
        /// Returns (true, "") on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> SubmitFeedbackAsync(string token, int ticketId, int rating, string? message)
        {
            var payload = new { rating, message };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tickets/{ticketId}/feedback") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls GET /api/tickets/{id}/feedback — the recorded feedback
        /// for one ticket, or null if none was ever submitted.</summary>
        public async Task<FeedbackEntryDto?> GetFeedbackForTicketAsync(string token, int ticketId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/tickets/{ticketId}/feedback");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                string body = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body) || body == "null")
                    return null;

                return JsonSerializer.Deserialize<FeedbackEntryDto>(body, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        /// <summary>Calls GET /api/feedback — every piece of feedback recorded
        /// across this tenant, for the Reports page. Returns an empty list if the
        /// request fails.</summary>
        public async Task<List<FeedbackEntryDto>> GetAllFeedbackAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/feedback");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<FeedbackEntryDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<FeedbackEntryDto>>(body, _jsonOptions) ?? new List<FeedbackEntryDto>();
            }
            catch (HttpRequestException)
            {
                return new List<FeedbackEntryDto>();
            }
        }

        /// <summary>Calls GET /api/customers/search — finds returning customers by
        /// name/phone/email within the caller's tenant, so a new ticket can be
        /// attached to them instead of creating a duplicate Customer row.</summary>
        public async Task<List<CustomerSearchResultDto>> SearchCustomersAsync(string token, string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
                return new List<CustomerSearchResultDto>();

            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"/api/customers/search?query={Uri.EscapeDataString(query.Trim())}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<CustomerSearchResultDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<CustomerSearchResultDto>>(body, _jsonOptions) ?? new List<CustomerSearchResultDto>();
            }
            catch (HttpRequestException)
            {
                return new List<CustomerSearchResultDto>();
            }
        }

        /// <summary>
        /// Calls POST /api/tickets to create a real Customer + ServiceTicket
        /// in the caller's own tenant database, attributed to their Staff record.
        /// </summary>
        public async Task<(bool Success, string Message, TicketDto? Ticket)> CreateTicketAsync(
            string token, string firstName, string middleName, string lastName,
            string phone, string email, string deviceName, string concern, decimal estimatedCost,
            IEnumerable<SelectedPartDto>? selectedParts = null, int? existingCustomerId = null, int? promotionId = null,
            decimal? originalPrice = null)
        {
            var payload = new
            {
                firstName,
                middleName,
                lastName,
                phone,
                email,
                deviceName,
                concern,
                estimatedCost,
                selectedParts = (selectedParts ?? Enumerable.Empty<SelectedPartDto>())
                    .Select(p => new { name = p.Name, cost = p.Cost }),
                existingCustomerId,
                promotionId,
                originalPrice
            };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tickets") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, CleanErrorMessage(body), null);

                var ticket = JsonSerializer.Deserialize<TicketDto>(body, _jsonOptions);
                return (true, "Created.", ticket);
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.", null);
            }
        }

        /// <summary>
        /// Calls GET /api/tickets, scoped to the caller's tenant.
        /// status: "pending", "history", or null for everything.
        /// Returns an empty list if the request fails for any reason.
        /// </summary>
        public async Task<List<TicketDto>> GetTicketsAsync(string token, string? status = null)
        {
            string url = status is null ? "/api/tickets" : $"/api/tickets?status={status}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<TicketDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TicketDto>>(body, _jsonOptions) ?? new List<TicketDto>();
            }
            catch (HttpRequestException)
            {
                return new List<TicketDto>();
            }
        }

        /// <summary>Calls GET /api/tickets/{id}/lookup — a single ticket by number,
        /// for the header's "Find ticket #" search. Null if it doesn't exist
        /// (or is archived).</summary>
        public async Task<TicketLookupDto?> LookupTicketAsync(string token, int ticketId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/tickets/{ticketId}/lookup");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TicketLookupDto>(body, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        /// <summary>Calls GET /api/tickets/today-summary — tenant-wide counts for
        /// the small "today" strip on Pending Customers.</summary>
        public async Task<TodaySummaryDto> GetTodaySummaryAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/tickets/today-summary");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new TodaySummaryDto();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TodaySummaryDto>(body, _jsonOptions) ?? new TodaySummaryDto();
            }
            catch (HttpRequestException)
            {
                return new TodaySummaryDto();
            }
        }

        /// <summary>Calls GET /api/staff/me — the logged-in Staff's own branch, if any.</summary>
        public async Task<StaffProfileDto> GetMyStaffProfileAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/staff/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new StaffProfileDto();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<StaffProfileDto>(body, _jsonOptions) ?? new StaffProfileDto();
            }
            catch (HttpRequestException)
            {
                return new StaffProfileDto();
            }
        }

        /// <summary>Calls PUT /api/tickets/{id}/approval — records diagnosis findings
        /// and, once known, the customer's decision. decision: "Pending", "Approved",
        /// or "Rejected". Returns (true, "") on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> RecordApprovalAsync(
            string token, int ticketId, string diagnosisFindings, decimal estimatedCost, string decision,
            decimal? approvedAmount = null, string? method = null)
        {
            var payload = new { diagnosisFindings, estimatedCost, decision, approvedAmount, method };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tickets/{ticketId}/approval") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/tickets/{id}/status. Returns (true, "") on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> UpdateTicketStatusAsync(string token, int ticketId, string status)
        {
            var payload = new { status };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tickets/{ticketId}/status") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/tickets/{id}/pickup. Returns (true, "") on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> UpdateTicketPickupStatusAsync(string token, int ticketId, bool pickedUp)
        {
            var payload = new { pickedUp };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tickets/{ticketId}/pickup") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/tickets/{id}/payment. amountPaid is required
        /// (and only used) when paymentStatus is "Partial" — the server derives it
        /// automatically for "Paid"/"Unpaid". Returns (true, "") on success,
        /// (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> UpdateTicketPaymentStatusAsync(string token, int ticketId, string paymentStatus, decimal? amountPaid = null)
        {
            var payload = new { paymentStatus, amountPaid };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tickets/{ticketId}/payment") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/tickets/{id} to fix the customer's name/phone/
        /// email, or the device name, concern, or estimated cost. Returns (true, "")
        /// on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> UpdateTicketDetailsAsync(
            string token, int ticketId, string fullName, string phone, string email,
            string deviceName, string concern, decimal estimatedCost,
            IEnumerable<SelectedPartDto>? selectedParts = null, int? promotionId = null, bool clearPromotion = false,
            decimal? originalPrice = null)
        {
            var payload = new
            {
                fullName,
                phone,
                email,
                deviceName,
                concern,
                estimatedCost,
                selectedParts = (selectedParts ?? Enumerable.Empty<SelectedPartDto>())
                    .Select(p => new { name = p.Name, cost = p.Cost }),
                promotionId,
                clearPromotion,
                originalPrice
            };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tickets/{ticketId}") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/tickets/{id}/archive. This hides the ticket from
        /// Pending/History — never a real delete. Returns (true, "") on success,
        /// (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> ArchiveTicketAsync(string token, int ticketId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tickets/{ticketId}/archive");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls POST /api/promotions to draft or submit a new promotion.
        /// Returns (true, "", dto) on success, (false, errorMessage, null) on failure.</summary>
        public async Task<(bool Success, string Message, PromotionDto? Promotion)> CreatePromotionAsync(
            string token, string title, string type, string message, string reason, bool submitForApproval)
        {
            var payload = new { title, type, message, reason, submitForApproval };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/promotions") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, CleanErrorMessage(body), null);

                var promotion = JsonSerializer.Deserialize<PromotionDto>(body, _jsonOptions);
                return (true, "Created.", promotion);
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.", null);
            }
        }

        /// <summary>Calls GET /api/promotions. mine=true returns only the caller's
        /// own submissions; false returns every promotion in the tenant. Returns an
        /// empty list if the request fails for any reason.</summary>
        public async Task<List<PromotionDto>> GetPromotionsAsync(string token, bool mine)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/promotions?mine={mine}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<PromotionDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<PromotionDto>>(body, _jsonOptions) ?? new List<PromotionDto>();
            }
            catch (HttpRequestException)
            {
                return new List<PromotionDto>();
            }
        }

        /// <summary>Calls PUT /api/promotions/{id}/approve or /reject. Returns
        /// (true, "") on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> ReviewPromotionAsync(string token, int promotionId, bool approve)
        {
            string action = approve ? "approve" : "reject";
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/promotions/{promotionId}/{action}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/promotions/{id}/archive — hides a promotion
        /// from Admin's review list, without deleting it.</summary>
        public async Task<(bool Success, string Message)> ArchivePromotionAsync(string token, int promotionId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/promotions/{promotionId}/archive");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        // ===================== Sync status =====================

        public class SyncStatusDto
        {
            [JsonPropertyName("state")]
            public string State { get; set; } = "Unknown";

            [JsonPropertyName("pendingCount")]
            public int PendingCount { get; set; }

            [JsonPropertyName("lastSyncedAt")]
            public DateTime? LastSyncedAt { get; set; }
        }

        /// <summary>Calls GET /api/sync/status — the caller's own tenant's
        /// local-first sync state, for the topbar indicator. Returns a
        /// "Unknown" status (rather than null) on failure, so the indicator
        /// always has something safe to show.</summary>
        public async Task<SyncStatusDto> GetSyncStatusAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sync/status");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new SyncStatusDto();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SyncStatusDto>(body, _jsonOptions) ?? new SyncStatusDto();
            }
            catch (HttpRequestException)
            {
                return new SyncStatusDto();
            }
        }

        // ===================== Branch =====================

        /// <summary>Calls GET /api/branch — every branch this tenant has, or null
        /// if this tenant has no Branch Management at all (used to decide
        /// whether the nav item shows up) — an empty (non-null) list means
        /// Branch Management is available but no branch has been added yet.</summary>
        public async Task<List<BranchDto>?> GetBranchesAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/branch");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<BranchDto>>(body, _jsonOptions) ?? new List<BranchDto>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        /// <summary>Calls POST /api/branch — adds a new branch to this tenant.
        /// Returns (true, "", branch) on success, (false, errorMessage, null) on failure.</summary>
        public async Task<(bool Success, string Message, BranchDto? Branch)> CreateBranchAsync(string token, string name, string address, string phone, string email)
        {
            var payload = new { name, address, phone, email };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/branch") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, CleanErrorMessage(body), null);

                return (true, "", JsonSerializer.Deserialize<BranchDto>(body, _jsonOptions));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.", null);
            }
        }

        /// <summary>Calls PUT /api/branch/{id} — updates one of this tenant's branches.
        /// Returns (true, "") on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> UpdateBranchAsync(string token, int branchId, string name, string address, string phone, string email)
        {
            var payload = new { name, address, phone, email };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/branch/{branchId}") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/branch/{id}/archive — hides a branch from
        /// Branch Management and the branch picker, without deleting it.
        /// Returns (true, "") on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> ArchiveBranchAsync(string token, int branchId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/branch/{branchId}/archive");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        // ===================== Tenant Terms & Conditions =====================
        // A tenant's own version history — separate from GetTermsVersionsAsync/
        // PublishTermsVersionAsync above, which are Superadmin's platform-wide
        // Terms & Agreement.

        /// <summary>Calls GET /api/terms — every version this tenant has
        /// published, newest first. Admin only. Returns an empty list if the
        /// request fails.</summary>
        public async Task<List<TenantTermsVersionDto>> GetTenantTermsVersionsAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/terms");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<TenantTermsVersionDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TenantTermsVersionDto>>(body, _jsonOptions) ?? new List<TenantTermsVersionDto>();
            }
            catch (HttpRequestException)
            {
                return new List<TenantTermsVersionDto>();
            }
        }

        /// <summary>Calls POST /api/terms — publishes a new version for this
        /// tenant and makes it current. Admin only. Returns (true, "") on
        /// success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> PublishTenantTermsVersionAsync(string token, string version, string content)
        {
            var payload = new { version, content };
            string json = JsonSerializer.Serialize(payload);

            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/terms") { Content = httpContent };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/terms/{id}/activate — makes an
        /// already-published version current again, e.g. rolling back after a
        /// bad edit, without republishing it as a "new" version. Admin only.</summary>
        public async Task<(bool Success, string Message)> SetCurrentTermsVersionAsync(string token, int termsId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/terms/{termsId}/activate");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls GET /api/terms/current — this tenant's currently
        /// published terms content, or null if none has been published yet.
        /// Callable by any authenticated tenant role (Staff/Manager included) —
        /// this is what the Add Customer screen shows.</summary>
        public async Task<string?> GetCurrentTermsAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/terms/current");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CurrentTermsDto>(body, _jsonOptions)?.Content;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // ===================== Superadmin =====================

        /// <summary>Calls GET /api/superadmin/subscriptions — every active tenant's
        /// subscription/billing status. Returns an empty list if the request fails.</summary>
        public async Task<List<TenantSubscriptionEntryDto>> GetSubscriptionsAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/superadmin/subscriptions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<TenantSubscriptionEntryDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TenantSubscriptionEntryDto>>(body, _jsonOptions) ?? new List<TenantSubscriptionEntryDto>();
            }
            catch (HttpRequestException)
            {
                return new List<TenantSubscriptionEntryDto>();
            }
        }

        /// <summary>Calls PUT /api/superadmin/subscriptions/{companyId}/status —
        /// suspends or reactivates a tenant's Admin login. status must be "Active"
        /// or "Suspended". Returns (true, serverMessage) on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> SetSubscriptionStatusAsync(string token, int companyId, string status)
        {
            var payload = new { status };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/superadmin/subscriptions/{companyId}/status") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, body.Trim('"'));

                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/superadmin/subscriptions/{companyId}/billing-cycle
        /// — updates a tenant's billing cycle bookkeeping. billingCycle must be
        /// "Monthly" or "Yearly". Returns (true, serverMessage) on success,
        /// (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> SetBillingCycleAsync(string token, int companyId, string billingCycle, DateTime? nextBillingDate)
        {
            var payload = new { billingCycle, nextBillingDate };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/superadmin/subscriptions/{companyId}/billing-cycle") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, body.Trim('"'));

                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/superadmin/subscriptions/{companyId}/amount —
        /// how much this tenant pays per billing cycle. amount: null clears it.</summary>
        public async Task<(bool Success, string Message)> SetSubscriptionAmountAsync(string token, int companyId, decimal? amount)
        {
            var payload = new { amount };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/superadmin/subscriptions/{companyId}/amount") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, body.Trim('"'));

                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/superadmin/subscriptions/{companyId}/plan —
        /// sets which module set this tenant's own apps unlock. Plan must be
        /// "AllAccess", "Growth", or "Starter". Returns (true, "") on success,
        /// (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> SetSubscriptionPlanAsync(string token, int companyId, string plan)
        {
            var payload = new { plan };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/superadmin/subscriptions/{companyId}/plan") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, body.Trim('"'));

                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls GET /api/modules — which sidebar modules the caller's
        /// own tenant currently has, per its subscription plan. Always returns
        /// a usable DTO (defaults to every module true) even if the request
        /// fails, so a network hiccup never hides a module that should show.</summary>
        public async Task<ModuleAccessDto> GetModuleAccessAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/modules");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new ModuleAccessDto();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ModuleAccessDto>(body, _jsonOptions) ?? new ModuleAccessDto();
            }
            catch (HttpRequestException)
            {
                return new ModuleAccessDto();
            }
        }

        /// <summary>Calls GET /api/superadmin/terms — every published Terms &amp;
        /// Agreement version, newest first. Returns an empty list if the request fails.</summary>
        public async Task<List<TermsVersionEntryDto>> GetTermsVersionsAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/superadmin/terms");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<TermsVersionEntryDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TermsVersionEntryDto>>(body, _jsonOptions) ?? new List<TermsVersionEntryDto>();
            }
            catch (HttpRequestException)
            {
                return new List<TermsVersionEntryDto>();
            }
        }

        /// <summary>Calls POST /api/superadmin/terms — publishes a new Terms &amp;
        /// Agreement version and makes it current. Returns (true, "") on success,
        /// (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> PublishTermsVersionAsync(string token, string version, string content, string planScope)
        {
            var payload = new { version, content, planScope };
            string json = JsonSerializer.Serialize(payload);

            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/superadmin/terms") { Content = httpContent };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/superadmin/terms/{id}/scope — moves an
        /// already-published version to a different plan without republishing
        /// it. Returns (true, "") on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> UpdateTermsVersionScopeAsync(string token, int termsId, string planScope)
        {
            var payload = new { planScope };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/superadmin/terms/{termsId}/scope") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls GET /api/superadmin/terms/current/{plan} — the terms
        /// version currently in effect for that plan (the plan-specific current
        /// version, or the "All plans" one as a fallback). Returns null if
        /// nothing has ever been published for it, or if the request fails.</summary>
        public async Task<TermsVersionEntryDto?> GetCurrentTermsForPlanAsync(string token, string plan)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/superadmin/terms/current/{plan}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TermsVersionEntryDto>(body, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        /// <summary>Calls GET /api/superadmin/admins — the one Admin account per
        /// tenant, across the whole platform. Returns an empty list if the request fails.</summary>
        public async Task<List<SuperadminAdminAccountEntryDto>> GetSuperadminAdminAccountsAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/superadmin/admins");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<SuperadminAdminAccountEntryDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<SuperadminAdminAccountEntryDto>>(body, _jsonOptions) ?? new List<SuperadminAdminAccountEntryDto>();
            }
            catch (HttpRequestException)
            {
                return new List<SuperadminAdminAccountEntryDto>();
            }
        }

        /// <summary>Calls PUT /api/superadmin/admins/{username}/reset-password —
        /// sets a new password for an Admin account. Returns (true, serverMessage)
        /// on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> SuperadminResetPasswordAsync(string token, string username, string newPassword)
        {
            var payload = new { newPassword };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/superadmin/admins/{Uri.EscapeDataString(username)}/reset-password") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, body.Trim('"'));

                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/superadmin/admins/{username}/status — suspends
        /// or reactivates an Admin account. status must be "Active" or "Suspended".
        /// Returns (true, serverMessage) on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> SuperadminSetAccountStatusAsync(string token, string username, string status)
        {
            var payload = new { status };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/superadmin/admins/{Uri.EscapeDataString(username)}/status") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, body.Trim('"'));

                return (false, CleanErrorMessage(body));
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls GET /api/superadmin/platform-report — cross-tenant
        /// aggregate figures for the last 30 days. Returns null if the request fails.</summary>
        public async Task<PlatformReportResponseDto?> GetPlatformReportAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/superadmin/platform-report");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PlatformReportResponseDto>(body, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}