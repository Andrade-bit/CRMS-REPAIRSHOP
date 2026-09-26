using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// Ticket actions only Admin's screen calls today: archiving. Same route prefix
    /// as StaffTicketsController/TicketsController — see the note there about why
    /// that's fine (each action's full route+verb is what has to stay unique).
    /// </summary>
    [ApiController]
    [Route("api/tickets")]
    [Authorize]
    public class AdminTicketsController : ControllerBase
    {
        private readonly TenantCRMSDbContext _tenantDb;

        public AdminTicketsController(TenantCRMSDbContext tenantDb)
        {
            _tenantDb = tenantDb;
        }

        /// <summary>Hides a ticket from Pending Customers / Customer History. This is
        /// never a real delete — ArchivedAt is just set to now, so the row (and the
        /// who/when accountability trail on it) stays in the database.</summary>
        [HttpPut("{id}/archive")]
        public async Task<IActionResult> ArchiveTicket(int id)
        {
            var ticket = await _tenantDb.ServiceTickets.FirstOrDefaultAsync(t => t.TicketId == id);
            if (ticket is null)
                return NotFound();

            ticket.ArchivedAt = DateTime.UtcNow;
            await _tenantDb.SaveChangesAsync();

            return Ok();
        }
    }
}
