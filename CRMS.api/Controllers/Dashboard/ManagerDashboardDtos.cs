namespace CRMS.api.Controllers
{
    public record PromotionStatusEntry(string Status, int Count);
    public record PromotionSummaryEntry(int PromotionId, string Title, string Type, string Status, DateTime CreatedAt);

    public record ManagerDashboardResponse(
        int MyActiveTickets,
        int AwaitingCustomerApproval,
        int PromotionsAwaitingAdmin,
        int PromotionsApprovedMtd,
        List<PipelineEntry> MyTicketPipeline,
        List<PromotionStatusEntry> MyPromotionsBreakdown,
        List<PromotionSummaryEntry> RecentPromotions);
}
