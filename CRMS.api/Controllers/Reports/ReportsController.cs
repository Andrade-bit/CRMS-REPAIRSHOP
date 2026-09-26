using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// Backs the Admin Reports page. Every endpoint here is a real aggregate
    /// over this tenant's own ServiceTickets — nothing is sample/placeholder
    /// data. Split into three reports:
    ///  - Monthly Sales: resolved-ticket revenue per month, each month
    ///    drillable into the tickets that made it up (same pattern as the
    ///    Dashboard's KPI tiles).
    ///  - Top Customers: repeat-visit and total-spend ranking — the
    ///    "buying behavior" view for a repair shop (who comes back, how much
    ///    they've paid), since this app has no retail line items to analyze.
    ///  - Common Repairs: which device types and stated concerns show up most
    ///    often across resolved tickets.
    /// </summary>
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private static readonly string[] RevenueStatuses = { "Repaired", "Cancelled", "Unrepairable" };

        private readonly TenantCRMSDbContext _tenantDb;

        public ReportsController(TenantCRMSDbContext tenantDb)
        {
            _tenantDb = tenantDb;
        }

        [HttpGet("monthly-sales")]
        public async Task<ActionResult<List<MonthlySalesEntry>>> GetMonthlySales([FromQuery] int months = 12)
        {
            months = Math.Clamp(months, 1, 24);
            var rangeStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(months - 1));

            var resolved = await _tenantDb.ServiceTickets
                .Where(t => RevenueStatuses.Contains(t.Status) && t.CompletedAt != null && t.CompletedAt >= rangeStart)
                .Select(t => new { t.CompletedAt, t.EstimatedCost })
                .ToListAsync();

            var byMonth = resolved
                .GroupBy(t => new { t.CompletedAt!.Value.Year, t.CompletedAt!.Value.Month })
                .ToDictionary(g => g.Key, g => (Revenue: g.Sum(t => t.EstimatedCost), Count: g.Count()));

            var result = Enumerable.Range(0, months)
                .Select(i => rangeStart.AddMonths(i))
                .Select(month =>
                {
                    var key = new { month.Year, month.Month };
                    var stats = byMonth.TryGetValue(key, out var v) ? v : (Revenue: 0m, Count: 0);
                    return new MonthlySalesEntry(month.Year, month.Month, stats.Revenue, stats.Count);
                })
                .ToList();

            return Ok(result);
        }

        [HttpGet("monthly-sales/{year:int}/{month:int}")]
        public async Task<ActionResult<MonthlySalesDetail>> GetMonthlySalesDetail(int year, int month)
        {
            if (month is < 1 or > 12) return BadRequest("Month must be between 1 and 12.");

            var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            var tickets = await _tenantDb.ServiceTickets
                .Where(t => RevenueStatuses.Contains(t.Status) && t.CompletedAt != null && t.CompletedAt >= monthStart && t.CompletedAt < monthEnd)
                .Include(t => t.Customer)
                .OrderByDescending(t => t.CompletedAt)
                .Select(t => new TicketSummaryEntry(t.TicketId, t.Customer.FullName, t.DeviceName, t.EstimatedCost, t.CompletedAt, t.Status))
                .ToListAsync();

            decimal total = tickets.Sum(t => t.Amount);
            return Ok(new MonthlySalesDetail(year, month, total, tickets));
        }

        [HttpGet("top-customers")]
        public async Task<ActionResult<List<TopCustomerEntry>>> GetTopCustomers([FromQuery] int take = 15)
        {
            take = Math.Clamp(take, 1, 100);

            // Grouping by a joined navigation's property (Customer.FullName)
            // doesn't translate to SQL cleanly, so pull the flat fields first
            // and group in memory — same pattern already used for staff
            // workload on the Dashboard.
            var resolvedTickets = await _tenantDb.ServiceTickets
                .Where(t => RevenueStatuses.Contains(t.Status))
                .Include(t => t.Customer)
                .Select(t => new { t.CustomerId, t.Customer.FullName, t.EstimatedCost, t.CompletedAt, t.CreatedAt })
                .ToListAsync();

            var topCustomers = resolvedTickets
                .GroupBy(t => new { t.CustomerId, t.FullName })
                .Select(g => new TopCustomerEntry(
                    g.Key.CustomerId,
                    g.Key.FullName,
                    g.Count(),
                    g.Sum(t => t.EstimatedCost),
                    g.Max(t => t.CompletedAt ?? t.CreatedAt)))
                .OrderByDescending(c => c.TotalSpent)
                .ThenByDescending(c => c.VisitCount)
                .Take(take)
                .ToList();

            return Ok(topCustomers);
        }

        [HttpGet("common-devices")]
        public async Task<ActionResult<List<CommonValueEntry>>> GetCommonDevices([FromQuery] int take = 10)
        {
            take = Math.Clamp(take, 1, 50);

            // Pull the flat column first and group in memory — the same pattern
            // already used for staff workload/top customers — so this can never
            // depend on how a given EF Core version chooses to translate a
            // GroupBy + record-constructor projection straight into SQL.
            var deviceNames = await _tenantDb.ServiceTickets
                .Where(t => t.DeviceName != "")
                .Select(t => t.DeviceName)
                .ToListAsync();

            var result = deviceNames
                .GroupBy(name => name)
                .Select(g => new CommonValueEntry(g.Key, g.Count()))
                .OrderByDescending(e => e.Count)
                .Take(take)
                .ToList();

            return Ok(result);
        }

        [HttpGet("common-concerns")]
        public async Task<ActionResult<List<CommonValueEntry>>> GetCommonConcerns([FromQuery] int take = 10)
        {
            take = Math.Clamp(take, 1, 50);

            var concerns = await _tenantDb.ServiceTickets
                .Where(t => t.Concern != "")
                .Select(t => t.Concern)
                .ToListAsync();

            var result = concerns
                .GroupBy(name => name)
                .Select(g => new CommonValueEntry(g.Key, g.Count()))
                .OrderByDescending(e => e.Count)
                .Take(take)
                .ToList();

            return Ok(result);
        }

        /// <summary>The with-promotion / without-promotion split, over the same
        /// resolved-ticket scope Monthly Sales uses.</summary>
        private record TicketPromoInfo(int? PromotionId, decimal EstimatedCost, decimal? OriginalPrice);

        private static PromotionComparisonSide BuildComparisonSide(List<TicketPromoInfo> list)
        {
            int count = list.Count;
            decimal revenue = list.Sum(t => t.EstimatedCost);
            decimal discount = list.Sum(t => t.OriginalPrice.HasValue ? t.OriginalPrice.Value - t.EstimatedCost : 0m);
            decimal avg = count > 0 ? revenue / count : 0m;
            return new PromotionComparisonSide(count, avg, revenue, discount);
        }

        [HttpGet("promotion-comparison")]
        public async Task<ActionResult<PromotionComparisonResponse>> GetPromotionComparison()
        {
            var resolved = await _tenantDb.ServiceTickets
                .Where(t => RevenueStatuses.Contains(t.Status))
                .Select(t => new TicketPromoInfo(t.PromotionId, t.EstimatedCost, t.OriginalPrice))
                .ToListAsync();

            var withPromo = resolved.Where(t => t.PromotionId.HasValue).ToList();
            var withoutPromo = resolved.Where(t => !t.PromotionId.HasValue).ToList();

            return Ok(new PromotionComparisonResponse(
                BuildComparisonSide(withPromo),
                BuildComparisonSide(withoutPromo)));
        }

        /// <summary>Every Approved promotion's real track record — times used,
        /// discount given, and revenue generated. A promotion with zero uses
        /// still shows up here (at ₱0 across the board) since that's exactly as
        /// informative as one that flopped — never silently hidden.</summary>
        [HttpGet("promotion-effectiveness")]
        public async Task<ActionResult<List<PromotionEffectivenessEntry>>> GetPromotionEffectiveness()
        {
            var approvedPromotions = await _tenantDb.Promotions
                .Where(p => p.Status == PromotionStatus.Approved)
                .Select(p => new { p.PromotionId, p.Title, p.Type })
                .ToListAsync();

            var usedTickets = await _tenantDb.ServiceTickets
                .Where(t => RevenueStatuses.Contains(t.Status) && t.PromotionId != null)
                .Select(t => new { t.PromotionId, t.EstimatedCost, t.OriginalPrice })
                .ToListAsync();

            var result = approvedPromotions
                .Select(p =>
                {
                    var used = usedTickets.Where(t => t.PromotionId == p.PromotionId).ToList();
                    decimal revenue = used.Sum(t => t.EstimatedCost);
                    decimal discount = used.Sum(t => t.OriginalPrice.HasValue ? t.OriginalPrice.Value - t.EstimatedCost : 0m);
                    decimal avg = used.Count > 0 ? revenue / used.Count : 0m;
                    return new PromotionEffectivenessEntry(p.PromotionId, p.Title, p.Type, used.Count, discount, revenue, avg);
                })
                .OrderByDescending(e => e.RevenueGenerated)
                .ToList();

            return Ok(result);
        }
    }
}
