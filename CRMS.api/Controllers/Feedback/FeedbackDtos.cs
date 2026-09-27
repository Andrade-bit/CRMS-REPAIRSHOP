namespace CRMS.api.Controllers
{
    public record SubmitFeedbackRequest(int Rating, string? Message);

    /// <summary>One piece of customer feedback, with enough ticket context to
    /// make sense of it on the Reports page without a second lookup.</summary>
    public record FeedbackEntry(
        int FeedbackId, int TicketId, string CustomerName, string DeviceName,
        int Rating, string Message, DateTime CreatedAt, string HandledBy, DateTime TicketCreatedAt);
}
