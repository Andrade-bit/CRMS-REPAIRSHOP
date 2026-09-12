using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// Walk-in customer/service-ticket endpoints, scoped to whichever tenant
    /// the caller's JWT resolves to (TenantCRMSDbContext is injected already
    /// pointed at the right database — see Program.cs's registration).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly TenantCRMSDbContext _tenantDb;

        public TicketsController(TenantCRMSDbContext tenantDb)
        {
            _tenantDb = tenantDb;
        }

        public record CreateTicketRequest(
            string FirstName, string MiddleName, string LastName,
            string Phone, string Email, string DeviceName, string Concern, decimal EstimatedCost);

        public record TicketResponse(
            int TicketId, string CustomerName, string Phone, string Email,
            string DeviceName, string Concern, decimal EstimatedCost, string Status,
            DateTime CreatedAt, DateTime? CompletedAt, DateTime? PickedUpAt, string StaffName);

        public record UpdateStatusRequest(string Status);
        public record UpdatePickupRequest(bool PickedUp);

        [HttpPost]
        public async Task<ActionResult<TicketResponse>> CreateTicket(CreateTicketRequest request)
        {
            var staff = await FindCurrentStaffAsync();
            if (staff is null)
                return BadRequest("No Staff record found in this tenant for the logged-in account.");

            string fullName = string.Join(" ", new[] { request.FirstName, request.MiddleName, request.LastName }
                .Where(p => !string.IsNullOrWhiteSpace(p)));

            var customer = new Customer
            {
                AdminId = staff.AdminId,
                FullName = fullName,
                PhoneNumber = request.Phone,
                Email = request.Email ?? string.Empty
            };
            _tenantDb.Customers.Add(customer);

            var ticket = new ServiceTicket
            {
                Customer = customer,
                StaffId = staff.StaffId,
                DeviceName = request.DeviceName,
                Concern = request.Concern,
                Status = "Diagnose",
                EstimatedCost = request.EstimatedCost,
                TermsAccepted = true
            };
            _tenantDb.ServiceTickets.Add(ticket);

            await _tenantDb.SaveChangesAsync();

            return Ok(new TicketResponse(
                ticket.TicketId, customer.FullName, customer.PhoneNumber, customer.Email,
                ticket.DeviceName, ticket.Concern, ticket.EstimatedCost, ticket.Status,
                ticket.CreatedAt, ticket.CompletedAt, ticket.PickedUpAt, staff.Name));
        }

        /// <summary>status: "pending" (not yet Repaired), "history" (Repaired), or omitted for everything.</summary>
        [HttpGet]
        public async Task<ActionResult<List<TicketResponse>>> GetTickets([FromQuery] string? status)
        {
            var query = _tenantDb.ServiceTickets
                .Include(t => t.Customer)
                .Include(t => t.Staff)
                .AsQueryable();

            if (string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
                query = query.Where(t => t.Status != "Repaired");
            else if (string.Equals(status, "history", StringComparison.OrdinalIgnoreCase))
                query = query.Where(t => t.Status == "Repaired");

            var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            var result = tickets.Select(t => new TicketResponse(
                t.TicketId, t.Customer.FullName, t.Customer.PhoneNumber, t.Customer.Email,
                t.DeviceName, t.Concern, t.EstimatedCost, t.Status,
                t.CreatedAt, t.CompletedAt, t.PickedUpAt, t.Staff?.Name ?? "")).ToList();

            return Ok(result);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateStatusRequest request)
        {
            var ticket = await _tenantDb.ServiceTickets
                .Include(t => t.Customer)
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket is null)
                return NotFound();

            ticket.Status = request.Status;
            if (request.Status == "Repaired")
                ticket.CompletedAt = DateTime.UtcNow;

            await _tenantDb.SaveChangesAsync();

            // TODO: this is where a real email/SMS send would go, using
            // ticket.Customer.Email / ticket.Customer.PhoneNumber.

            return Ok();
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

        private async Task<Staff?> FindCurrentStaffAsync()
        {
            string? userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return null;

            return await _tenantDb.Staff.FirstOrDefaultAsync(s => s.ApplicationUserId == userId);
        }
    }
}
