namespace CRMS.api.Controllers
{
    // Shared by StaffTicketsController, AdminTicketsController, and TicketsController —
    // pulled out here once all three needed the same shapes, instead of nesting them
    // inside just one of the controllers.

    public record SelectedPart(string Name, decimal Cost);

    // ExistingCustomerId: when set, the ticket is linked to that returning
    // customer instead of creating a new Customer row — FirstName/MiddleName/
    // LastName/Phone/Email are ignored in that case.
    // PromotionId: an Approved promotion the customer used on this ticket — purely
    // a record of which one applied; Staff still enters the discounted
    // EstimatedCost themselves since a Promotion is just offer copy, not a
    // computable discount.
    // OriginalPrice: the price before that discount — optional, only meaningful
    // alongside a PromotionId. EstimatedCost is still the real, final agreed
    // price (unchanged meaning everywhere else in the app); OriginalPrice is
    // purely the "what it would have cost without the promotion" record.
    public record CreateTicketRequest(
        string FirstName, string MiddleName, string LastName,
        string Phone, string Email, string DeviceName, string Concern, decimal EstimatedCost,
        List<SelectedPart>? SelectedParts = null, int? ExistingCustomerId = null, int? PromotionId = null,
        decimal? OriginalPrice = null);

    public record CustomerSearchResult(int CustomerId, string FullName, string PhoneNumber, string Email, int TicketCount);

    public record TicketResponse(
        int TicketId, string CustomerName, string Phone, string Email,
        string DeviceName, string Concern, decimal EstimatedCost, string Status,
        DateTime CreatedAt, DateTime? CompletedAt, DateTime? PickedUpAt, DateTime? ArchivedAt, string StaffName,
        List<SelectedPart> SelectedParts,
        string? DiagnosisFindings, string ApprovalStatus, decimal? ApprovedAmount,
        DateTime? ApprovalDecisionAt, string? ApprovalMethod, string PaymentStatus, decimal AmountPaid,
        int? PromotionId, string? PromotionTitle, decimal? OriginalPrice, decimal? DiscountAmount,
        int? FeedbackRating, string? FeedbackMessage,
        int? BranchId = null, string? BranchName = null);

    public record UpdateStatusRequest(string Status);
    public record UpdatePickupRequest(bool PickedUp);

    // AmountPaid is only required (and only used) when PaymentStatus is
    // "Partial" — "Paid" always sets it to the full EstimatedCost and "Unpaid"
    // always resets it to 0, so there's no way for the two to disagree.
    public record UpdatePaymentRequest(string PaymentStatus, decimal? AmountPaid = null);
    public record UpdateTicketDetailsRequest(
        string FullName, string Phone, string Email,
        string DeviceName, string Concern, decimal EstimatedCost,
        List<SelectedPart>? SelectedParts = null, int? PromotionId = null, bool ClearPromotion = false,
        decimal? OriginalPrice = null);

    // Decision: "Pending" (findings recorded, still awaiting the customer's
    // decision — status becomes AwaitingApproval), "Approved" (status becomes
    // Repairing), or "Rejected" (status becomes Cancelled).
    public record RecordApprovalRequest(
        string DiagnosisFindings, decimal EstimatedCost, string Decision,
        decimal? ApprovedAmount = null, string? Method = null);
}
