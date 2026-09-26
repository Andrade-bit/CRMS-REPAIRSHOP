using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// Customers don't have their own login in this app, so feedback is always
    /// entered by whoever is handling the ticket (Staff, usually right at
    /// pickup) — this just records it. One feedback entry per ticket: submitting
    /// again updates the existing one rather than creating a duplicate.
    /// </summary>
    [ApiController]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly TenantCRMSDbContext _tenantDb;

        public FeedbackController(TenantCRMSDbContext tenantDb)
        {
            _tenantDb = tenantDb;
        }

        [HttpPut("api/tickets/{id}/feedback")]
        public async Task<IActionResult> SubmitFeedback(int id, SubmitFeedbackRequest request)
        {
            if (request.Rating is < 1 or > 5)
                return BadRequest("Rating must be between 1 and 5.");

            var ticket = await _tenantDb.ServiceTickets.FirstOrDefaultAsync(t => t.TicketId == id);
            if (ticket is null)
                return NotFound();

            var existing = await _tenantDb.Feedbacks.FirstOrDefaultAsync(f => f.TicketId == id);
            if (existing is null)
            {
                _tenantDb.Feedbacks.Add(new Feedback
                {
                    TicketId = id,
                    Rating = request.Rating,
                    Message = request.Message ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Rating = request.Rating;
                existing.Message = request.Message ?? string.Empty;
                existing.CreatedAt = DateTime.UtcNow;
            }

            await _tenantDb.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("api/tickets/{id}/feedback")]
        public async Task<ActionResult<FeedbackEntry?>> GetFeedbackForTicket(int id)
        {
            var entry = await _tenantDb.Feedbacks
                .Include(f => f.Ticket).ThenInclude(t => t.Customer)
                .Where(f => f.TicketId == id)
                .Select(f => new FeedbackEntry(
                    f.FeedbackId, f.TicketId, f.Ticket.Customer.FullName, f.Ticket.DeviceName,
                    f.Rating, f.Message, f.CreatedAt))
                .FirstOrDefaultAsync();

            return Ok(entry);
        }

        [HttpGet("api/feedback")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<FeedbackEntry>>> GetAllFeedback()
        {
            var entries = await _tenantDb.Feedbacks
                .Include(f => f.Ticket).ThenInclude(t => t.Customer)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FeedbackEntry(
                    f.FeedbackId, f.TicketId, f.Ticket.Customer.FullName, f.Ticket.DeviceName,
                    f.Rating, f.Message, f.CreatedAt))
                .ToListAsync();

            return Ok(entries);
        }
    }
}
