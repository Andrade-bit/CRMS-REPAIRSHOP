namespace CRMS.api.Controllers
{
    /// <summary>One calendar month's resolved-ticket totals, for the Reports
    /// page's Monthly Sales chart. Click one to drill into MonthlySalesDetail.</summary>
    public record MonthlySalesEntry(int Year, int Month, decimal Revenue, int TicketCount);

    /// <summary>The individual resolved tickets behind one month's total —
    /// backs the drill-down when a Monthly Sales bar/row is clicked.</summary>
    public record MonthlySalesDetail(int Year, int Month, decimal Revenue, List<TicketSummaryEntry> Tickets);

    /// <summary>A single customer's repeat-visit and spend history — the
    /// "buying behavior" view for a repair shop: who keeps coming back and how
    /// much they've paid in total, not demographic data this app never collects.</summary>
    public record TopCustomerEntry(int CustomerId, string FullName, int VisitCount, decimal TotalSpent, DateTime LastVisit);

    /// <summary>How often a device type or a stated repair concern shows up
    /// across resolved tickets — the closest analogue to "most common items
    /// purchased" for a service business instead of a retail one.</summary>
    public record CommonValueEntry(string Value, int Count);

    /// <summary>One side of the with-promotion / without-promotion comparison —
    /// same resolved-ticket scope as Monthly Sales, just split by whether a
    /// promotion was applied.</summary>
    public record PromotionComparisonSide(int TicketCount, decimal AvgTicketValue, decimal TotalRevenue, decimal TotalDiscount);

    public record PromotionComparisonResponse(PromotionComparisonSide WithPromotion, PromotionComparisonSide WithoutPromotion);

    /// <summary>One approved promotion's real track record — how many times it
    /// was actually used, not just that it exists. A promotion nobody has used
    /// yet still appears here at 0, since that's exactly as informative as one
    /// that flopped.</summary>
    public record PromotionEffectivenessEntry(
        int PromotionId, string Title, string Type, int TimesUsed,
        decimal DiscountGiven, decimal RevenueGenerated, decimal AvgTicketValue);
}
