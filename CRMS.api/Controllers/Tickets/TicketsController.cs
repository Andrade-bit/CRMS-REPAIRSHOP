using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// Ticket actions BOTH Staff's and Admin's screens call — viewing tickets and
    /// marking pickup. Kept separate from StaffTicketsController/AdminTicketsController
    /// specifically because these two aren't exclusive to either role.
    /// </summary>
    [ApiController]
    [Route("api/tickets")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly TenantCRMSDbContext _tenantDb;

        public TicketsController(TenantCRMSDbContext tenantDb)
        {
            _tenantDb = tenantDb;
        }

        // A ticket is "terminal" once it reaches one of these — it's done being
        // actively worked, however it ended, and moves from Pending Customers to
        // Customer History. Kept in sync with CRMS.winforms.Controllers.TicketStatus.Terminal.
        private static readonly string[] TerminalStatuses = { "Repaired", "Cancelled", "Unrepairable" };

        /// <summary>status: "pending" (not yet in a terminal status), "history" (Repaired/
        /// Cancelled/Unrepairable), or omitted for everything.
        /// Archived tickets are always excluded — they're hidden, not part of any normal view.</summary>
        [HttpGet]
        public async Task<ActionResult<List<TicketResponse>>> GetTickets([FromQuery] string? status)
        {
            var query = _tenantDb.ServiceTickets
                .Include(t => t.Customer)
                .Include(t => t.Staff)
                .Include(t => t.RepairParts)
                .Include(t => t.Promotion)
                .Include(t => t.Feedbacks)
                .Include(t => t.Branch)
                .Where(t => t.ArchivedAt == null)
                .AsQueryable();

            int? branchFilter = await GetCallerBranchFilterAsync();
            if (branchFilter.HasValue)
                query = query.Where(t => t.BranchId == branchFilter.Value);

            if (string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
                query = query.Where(t => !TerminalStatuses.Contains(t.Status));
            else if (string.Equals(status, "history", StringComparison.OrdinalIgnoreCase))
                query = query.Where(t => TerminalStatuses.Contains(t.Status));

            var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            var result = tickets.Select(t =>
            {
                var feedback = t.Feedbacks.FirstOrDefault();
                return new TicketResponse(
                    t.TicketId, t.Customer.FullName, t.Customer.PhoneNumber, t.Customer.Email,
                    t.DeviceName, t.Concern, t.EstimatedCost, t.Status,
                    t.CreatedAt, t.CompletedAt, t.PickedUpAt, t.ArchivedAt, t.Staff?.Name ?? "",
                    t.RepairParts.Select(p => new SelectedPart(p.PartName, p.PartCost)).ToList(),
                    t.DiagnosisFindings, t.ApprovalStatus, t.ApprovedAmount, t.ApprovalDecisionAt, t.ApprovalMethod,
                    t.PaymentStatus, t.AmountPaid, t.PromotionId, t.Promotion?.Title,
                    t.OriginalPrice, t.OriginalPrice.HasValue ? t.OriginalPrice.Value - t.EstimatedCost : null,
                    feedback?.Rating, feedback?.Message,
                    t.BranchId, t.Branch?.Name);
            }).ToList();

            return Ok(result);
        }

        /// <summary>A single ticket by number, for the header's "Find ticket #"
        /// search — a customer calling in asking about their ticket shouldn't
        /// need Staff to page through the full history to answer.</summary>
        [HttpGet("{id}/lookup")]
        public async Task<ActionResult<TicketLookupResponse>> LookupTicket(int id)
        {
            var query = _tenantDb.ServiceTickets
                .Include(t => t.Customer)
                .Where(t => t.ArchivedAt == null)
                .AsQueryable();

            int? branchFilter = await GetCallerBranchFilterAsync();
            if (branchFilter.HasValue)
                query = query.Where(t => t.BranchId == branchFilter.Value);

            var ticket = await query
                .Select(t => new TicketLookupResponse(t.TicketId, t.Customer.FullName, t.DeviceName, t.Status, t.CreatedAt))
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket is null) return NotFound();
            return Ok(ticket);
        }

        /// <summary>Counts for the small "today" strip on Pending Customers —
        /// scoped the same way GetTickets is, so the numbers shown always match
        /// what that Staff/Manager can actually see in the list below them.</summary>
        [HttpGet("today-summary")]
        public async Task<ActionResult<TodaySummaryResponse>> GetTodaySummary()
        {
            var today = DateTime.UtcNow.Date;
            int? branchFilter = await GetCallerBranchFilterAsync();

            var query = _tenantDb.ServiceTickets.Where(t => t.ArchivedAt == null).AsQueryable();
            if (branchFilter.HasValue)
                query = query.Where(t => t.BranchId == branchFilter.Value);

            int ticketsToday = await query
                .CountAsync(t => t.CreatedAt >= today && t.CreatedAt < today.AddDays(1));

            int awaitingPickup = await query
                .CountAsync(t => TerminalStatuses.Contains(t.Status) && t.PickedUpAt == null);

            int needsApproval = await query
                .CountAsync(t => t.ApprovalStatus == "Pending");

            return Ok(new TodaySummaryResponse(ticketsToday, awaitingPickup, needsApproval));
        }

        /// <summary>Returns the caller's own BranchId to filter tickets by — but
        /// only when that's actually meaningful: the caller is a branch-assigned
        /// Staff or Manager. Admin, Superadmin, and any Staff/Manager not
        /// assigned to a specific branch all get null (no filter, see
        /// everything), matching how BranchId already works everywhere else —
        /// unassigned means "not scoped to one branch", not "scoped to none".</summary>
        private async Task<int?> GetCallerBranchFilterAsync()
        {
            if (!User.IsInRole("Staff") && !User.IsInRole("Manager"))
                return null;

            string? userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return null;

            var staff = await _tenantDb.Staff.FirstOrDefaultAsync(s => s.ApplicationUserId == userId);
            return staff?.BranchId;
        }

        [HttpPut("{id}/pickup")]
        public async Task<IActionResult> UpdatePickup(int id, UpdatePickupRequest request)
        {
            var ticket = await _tenantDb.ServiceTickets.FirstOrDefaultAsync(t => t.TicketId == id);
            if (ticket is null)
                return NotFound();

            ticket.PickedUpAt = request.PickedUp ? DateTime.UtcNow : null;
            await _tenantDb.SaveChangesAsync();

            return Ok();
        }

        private static readonly string[] ValidPaymentStatuses = { "Unpaid", "Partial", "Paid" };

        [HttpPut("{id}/payment")]
        public async Task<IActionResult> UpdatePayment(int id, UpdatePaymentRequest request)
        {
            if (!ValidPaymentStatuses.Contains(request.PaymentStatus))
                return BadRequest($"\"{request.PaymentStatus}\" is not a recognized payment status.");

            var ticket = await _tenantDb.ServiceTickets.FirstOrDefaultAsync(t => t.TicketId == id);
            if (ticket is null)
                return NotFound();

            // Once a customer has fully paid, that's a closed financial fact —
            // walking it back to Partial/Unpaid would misstate real revenue
            // already collected. Enforced here, not just by disabling the
            // control client-side, so a raw API call can't undo it either.
            if (ticket.PaymentStatus == "Paid" && request.PaymentStatus != "Paid")
                return BadRequest("This ticket is already fully paid — payment status can't be changed back.");

            // AmountPaid always stays consistent with PaymentStatus — Unpaid and
            // Paid derive it automatically, only Partial needs (and validates) a
            // real entered amount, so the two can never contradict each other.
            switch (request.PaymentStatus)
            {
                case "Unpaid":
                    ticket.AmountPaid = 0m;
                    break;

                case "Paid":
                    ticket.AmountPaid = ticket.EstimatedCost;
                    break;

                case "Partial":
                    if (request.AmountPaid is null || request.AmountPaid <= 0 || request.AmountPaid >= ticket.EstimatedCost)
                        return BadRequest($"Enter a partial amount greater than ₱0 and less than the estimated cost of ₱{ticket.EstimatedCost:0.00}.");
                    ticket.AmountPaid = request.AmountPaid.Value;
                    break;
            }

            ticket.PaymentStatus = request.PaymentStatus;
            await _tenantDb.SaveChangesAsync();

            return Ok();
        }
    }
}
