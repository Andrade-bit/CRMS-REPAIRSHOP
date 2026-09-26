namespace CRMS.api.Controllers
{
    public record RevenuePoint(DateTime Date, decimal Amount);
    public record PaymentBreakdownEntry(string PaymentStatus, decimal Amount, int TicketCount);
    public record PipelineEntry(string Status, int Count);
    public record StaffWorkloadEntry(string StaffName, int Count);

    // Backs the drill-down popup behind each KPI tile — the specific tickets/
    // promotions that make up that number, so clicking a tile never needs a
    // second round trip.
    public record TicketSummaryEntry(int TicketId, string CustomerName, string DeviceName, decimal Amount, DateTime? Date, string Status);
    public record PendingPromotionEntry(int PromotionId, string Title, string Type, string StaffName, DateTime CreatedAt);

    // How many of each star rating (1-5) this tenant has received — always all
    // five entries, zero-filled, so the dashboard can draw a bar for every
    // star without checking which ones actually exist.
    public record RatingBucketEntry(int Stars, int Count);

    public record DashboardResponse(
        decimal RevenueMtd,
        int ReadyNotPickedUp,
        int UnpaidTicketsCount,
        int PromotionsPendingCount,
        List<RevenuePoint> RevenueTrend,
        List<PaymentBreakdownEntry> PaymentBreakdown,
        List<PipelineEntry> TicketPipeline,
        List<StaffWorkloadEntry> StaffWorkload,
        List<TicketSummaryEntry> RevenueMtdTickets,
        List<TicketSummaryEntry> ReadyNotPickedUpTickets,
        List<TicketSummaryEntry> UnpaidTickets,
        List<PendingPromotionEntry> PendingPromotions,
        double AverageRating,
        int RatingCount,
        List<RatingBucketEntry> RatingDistribution,
        string? RecentFeedbackMessage,
        string? RecentFeedbackCustomer);
}
