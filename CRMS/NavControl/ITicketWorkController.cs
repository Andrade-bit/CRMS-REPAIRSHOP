using System.Collections.Generic;
using System.Threading.Tasks;
using CRMS.winforms.Api;

namespace CRMS.winforms.Controllers
{
    /// <summary>
    /// The ticket operations PendingCustomersControl needs. Both StaffController
    /// and ManagerController implement this, since Manager does the exact same
    /// day-to-day ticket work as Staff — this lets the same grid/dialog control be
    /// reused by both roles without either one being named after the other.
    /// </summary>
    public interface ITicketWorkController
    {
        Task<List<TicketRecord>> GetPendingTicketsAsync();

        /// <summary>Tenant-wide counts for the small "today" strip above the
        /// pending list — shared queue, so not scoped to whoever's logged in.</summary>
        Task<TodaySummaryDto> GetTodaySummaryAsync();

        /// <summary>Finds returning customers by name/phone/email, so AddCustomerAsync
        /// can attach the new ticket to one instead of creating a duplicate.</summary>
        Task<List<CustomerSearchResultDto>> SearchCustomersAsync(string query);

        Task<(bool Success, string Message, TicketDto? Ticket)> AddCustomerAsync(
            string firstName, string middleName, string lastName,
            string phoneNumber, string email, string deviceName, string concern, decimal estimatedCost,
            IEnumerable<TicketPart>? selectedParts = null, int? existingCustomerId = null, int? promotionId = null,
            decimal? originalPrice = null);

        /// <summary>Approved promotions the customer could be offered on this ticket.</summary>
        Task<List<PromotionDto>> GetApprovedPromotionsAsync();

        /// <summary>This tenant's currently published Terms &amp; Conditions
        /// content, shown on the Add Customer screen — null if the Admin
        /// hasn't published one yet.</summary>
        Task<string?> GetCurrentTermsAsync();

        Task<(bool Success, string Message)> UpdateTicketStatusAsync(int ticketId, string newStatus);

        /// <summary>Records diagnosis findings and, once known, the customer's approval
        /// decision ("Pending"/"Approved"/"Rejected") — the gate before Repairing.</summary>
        Task<(bool Success, string Message)> RecordApprovalAsync(
            int ticketId, string diagnosisFindings, decimal estimatedCost, string decision,
            decimal? approvedAmount = null, string? method = null);

        Task<(bool Success, string Message)> UpdateTicketDetailsAsync(
            int ticketId, string fullName, string phone, string email, string deviceName, string concern, decimal estimatedCost,
            IEnumerable<TicketPart>? selectedParts = null, int? promotionId = null, bool clearPromotion = false,
            decimal? originalPrice = null);

        /// <summary>Records how much (if anything) the customer has paid so far —
        /// available before a ticket is even finished, since a deposit or partial
        /// payment can come in while the device is still being worked on, not
        /// just once it lands in Customer History.</summary>
        Task<(bool Success, string Message)> UpdatePaymentStatusAsync(int ticketId, string paymentStatus, decimal? amountPaid = null);
    }
}
