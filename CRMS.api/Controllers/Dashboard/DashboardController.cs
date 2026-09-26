using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// Backs Admin's Dashboard page — every number here is a real aggregate over
    /// this tenant's own ServiceTickets/Promotions, not sample data. Only the
    /// active (non-terminal, non-archived) statuses this app actually uses feed
    /// the pipeline/workload breakdowns: Diagnose, AwaitingApproval, Repairing.
    ///
    /// Everything here is scoped to a [from, to] date range — from/to default to
    /// the start of the current calendar month through now, matching the
    /// Dashboard's default "This Month" filter. Every figure is scoped by
    /// whichever timestamp makes it a real answer to "what happened in this
    /// range": revenue/trend/ratings by CompletedAt or Feedback.CreatedAt,
    /// backlog-style counts (unpaid, still-in-progress pipeline/workload) by
    /// ServiceTicket.CreatedAt — i.e. "of what was opened in this range, how
    /// much is still outstanding right now."
    ///
    /// Two different ticket scopes are used deliberately regardless of range:
    ///  - "live" tickets (ArchivedAt == null) back operational to-do metrics
    ///    (ready for pickup, unpaid, active pipeline/workload) — archiving a
    ///    ticket correctly removes it from these, since it's no longer something
    ///    anyone needs to act on.
    ///  - "all" tickets (archived included) back financial totals (revenue) —
    ///    archiving a ticket must NOT erase real money it already earned from
    ///    the shop's historical totals. Revenue counts is any resolved outcome
    ///    (Repaired/Cancelled/Unrepairable), since a diagnostic fee on a
    ///    cancelled or unrepairable ticket is still real income.
    /// </summary>
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private static readonly string[] ActiveStatuses = { "Diagnose", "AwaitingApproval", "Repairing" };
        private static readonly string[] RevenueStatuses = { "Repaired", "Cancelled", "Unrepairable" };

        private readonly TenantCRMSDbContext _tenantDb;

        public DashboardController(TenantCRMSDbContext tenantDb)
        {
            _tenantDb = tenantDb;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardResponse>> GetDashboard([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? branchId)
        {
            var now = DateTime.UtcNow;
            var defaultStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // "to" is inclusive of the whole day the caller picked, so the
            // actual upper bound used in every query below is the exclusive
            // start of the next day. from/to arrive as plain calendar dates
            // (no timezone info) and every timestamp in this DB is already
            // UTC, so these are stamped Utc directly rather than converted —
            // ToUniversalTime() would wrongly reinterpret them as the server's
            // local time first.
            var rangeStart = from.HasValue ? DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc) : defaultStart;
            var rangeEndExclusive = to.HasValue ? DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc) : now;
            if (rangeEndExclusive <= rangeStart) rangeEndExclusive = rangeStart.AddDays(1);

            // Every ticket-derived metric below builds on these two base
            // queries, so filtering by branch here (when one's selected)
            // scopes the whole page in one place instead of repeating the
            // condition in every query that follows.
            var liveTickets = _tenantDb.ServiceTickets.Where(t => t.ArchivedAt == null && (branchId == null || t.BranchId == branchId));
            var allTickets = _tenantDb.ServiceTickets.Where(t => branchId == null || t.BranchId == branchId);

            decimal revenue = await allTickets
                .Where(t => RevenueStatuses.Contains(t.Status) && t.CompletedAt != null && t.CompletedAt >= rangeStart && t.CompletedAt < rangeEndExclusive)
                .SumAsync(t => (decimal?)t.EstimatedCost) ?? 0m;

            int readyNotPickedUp = await liveTickets
                .CountAsync(t => t.Status == "Repaired" && t.PickedUpAt == null &&
                    t.CompletedAt != null && t.CompletedAt >= rangeStart && t.CompletedAt < rangeEndExclusive);

            int unpaidTicketsCount = await liveTickets
                .CountAsync(t => t.PaymentStatus == "Unpaid" && t.CreatedAt >= rangeStart && t.CreatedAt < rangeEndExclusive);

            int promotionsPendingCount = await _tenantDb.Promotions
                .CountAsync(p => p.Status == "PendingApproval" && p.CreatedAt >= rangeStart && p.CreatedAt < rangeEndExclusive
                    && (branchId == null || p.Staff.BranchId == branchId));

            // The specific rows behind each KPI tile, for the drill-down popup —
            // capped at 50 so a busy shop's payload stays small.
            var revenueTickets = await allTickets
                .Where(t => RevenueStatuses.Contains(t.Status) && t.CompletedAt != null && t.CompletedAt >= rangeStart && t.CompletedAt < rangeEndExclusive)
                .Include(t => t.Customer)
                .OrderByDescending(t => t.CompletedAt)
                .Take(50)
                .Select(t => new TicketSummaryEntry(t.TicketId, t.Customer.FullName, t.DeviceName, t.EstimatedCost, t.CompletedAt, t.Status))
                .ToListAsync();

            var readyNotPickedUpTickets = await liveTickets
                .Where(t => t.Status == "Repaired" && t.PickedUpAt == null &&
                    t.CompletedAt != null && t.CompletedAt >= rangeStart && t.CompletedAt < rangeEndExclusive)
                .Include(t => t.Customer)
                .OrderByDescending(t => t.CompletedAt)
                .Take(50)
                .Select(t => new TicketSummaryEntry(t.TicketId, t.Customer.FullName, t.DeviceName, t.EstimatedCost, t.CompletedAt, t.Status))
                .ToListAsync();

            var unpaidTickets = await liveTickets
                .Where(t => t.PaymentStatus == "Unpaid" && t.CreatedAt >= rangeStart && t.CreatedAt < rangeEndExclusive)
                .Include(t => t.Customer)
                .OrderByDescending(t => t.CreatedAt)
                .Take(50)
                .Select(t => new TicketSummaryEntry(t.TicketId, t.Customer.FullName, t.DeviceName, t.EstimatedCost, t.CreatedAt, t.Status))
                .ToListAsync();

            var pendingPromotions = await _tenantDb.Promotions
                .Where(p => p.Status == "PendingApproval" && p.CreatedAt >= rangeStart && p.CreatedAt < rangeEndExclusive
                    && (branchId == null || p.Staff.BranchId == branchId))
                .Include(p => p.Staff)
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .Select(p => new PendingPromotionEntry(p.PromotionId, p.Title, p.Type, p.Staff.Name, p.CreatedAt))
                .ToListAsync();

            // Revenue trend across the whole selected range — bucket width scales
            // with the range's span so a year-long filter doesn't render 365
            // barely-visible daily points: daily up to 45 days, weekly up to
            // ~7 months, monthly beyond that.
            var completedInRange = await allTickets
                .Where(t => RevenueStatuses.Contains(t.Status) && t.CompletedAt != null && t.CompletedAt >= rangeStart && t.CompletedAt < rangeEndExclusive)
                .Select(t => new { t.CompletedAt, t.EstimatedCost })
                .ToListAsync();

            double totalDays = (rangeEndExclusive - rangeStart).TotalDays;
            int bucketDays = totalDays <= 45 ? 1 : totalDays <= 210 ? 7 : 30;

            var revenueTrend = new List<RevenuePoint>();
            for (var bucketStart = rangeStart; bucketStart < rangeEndExclusive; bucketStart = bucketStart.AddDays(bucketDays))
            {
                var bucketEnd = bucketStart.AddDays(bucketDays);
                if (bucketEnd > rangeEndExclusive) bucketEnd = rangeEndExclusive;

                decimal bucketAmount = completedInRange
                    .Where(t => t.CompletedAt!.Value >= bucketStart && t.CompletedAt!.Value < bucketEnd)
                    .Sum(t => t.EstimatedCost);

                revenueTrend.Add(new RevenuePoint(bucketStart, bucketAmount));
            }

            var paymentBreakdown = await allTickets
                .Where(t => t.CreatedAt >= rangeStart && t.CreatedAt < rangeEndExclusive)
                .GroupBy(t => t.PaymentStatus)
                .Select(g => new PaymentBreakdownEntry(g.Key, g.Sum(t => t.EstimatedCost), g.Count()))
                .ToListAsync();

            var ticketPipeline = await liveTickets
                .Where(t => ActiveStatuses.Contains(t.Status) && t.CreatedAt >= rangeStart && t.CreatedAt < rangeEndExclusive)
                .GroupBy(t => t.Status)
                .Select(g => new PipelineEntry(g.Key, g.Count()))
                .ToListAsync();

            // Grouping by a joined navigation's property here doesn't translate to
            // SQL cleanly, and staff counts are small anyway — group in memory.
            var activeStaffNames = await liveTickets
                .Where(t => ActiveStatuses.Contains(t.Status) && t.Staff != null && t.CreatedAt >= rangeStart && t.CreatedAt < rangeEndExclusive)
                .Select(t => t.Staff!.Name)
                .ToListAsync();

            var staffWorkload = activeStaffNames
                .GroupBy(name => name)
                .Select(g => new StaffWorkloadEntry(g.Key, g.Count()))
                .OrderByDescending(s => s.Count)
                .ToList();

            var ratings = await _tenantDb.Feedbacks
                .Where(f => f.CreatedAt >= rangeStart && f.CreatedAt < rangeEndExclusive
                    && (branchId == null || f.Ticket.BranchId == branchId))
                .Select(f => f.Rating)
                .ToListAsync();
            double averageRating = ratings.Count > 0 ? ratings.Average() : 0;
            var ratingDistribution = Enumerable.Range(1, 5)
                .Select(stars => new RatingBucketEntry(stars, ratings.Count(r => r == stars)))
                .OrderByDescending(b => b.Stars)
                .ToList();

            var recentFeedback = await _tenantDb.Feedbacks
                .Include(f => f.Ticket).ThenInclude(t => t.Customer)
                .Where(f => f.Message != "" && f.CreatedAt >= rangeStart && f.CreatedAt < rangeEndExclusive
                    && (branchId == null || f.Ticket.BranchId == branchId))
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new { f.Message, CustomerName = f.Ticket.Customer.FullName })
                .FirstOrDefaultAsync();

            return Ok(new DashboardResponse(
                revenue, readyNotPickedUp, unpaidTicketsCount, promotionsPendingCount,
                revenueTrend, paymentBreakdown, ticketPipeline, staffWorkload,
                revenueTickets, readyNotPickedUpTickets, unpaidTickets, pendingPromotions,
                averageRating, ratings.Count, ratingDistribution,
                recentFeedback?.Message, recentFeedback?.CustomerName));
        }
    }
}
