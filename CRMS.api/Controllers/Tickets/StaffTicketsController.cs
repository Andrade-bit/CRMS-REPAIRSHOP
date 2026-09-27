using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using CRMS.infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static CRMS.api.ContactValidation;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// Ticket actions only Staff's screens call today: creating a ticket, moving it
    /// through Diagnose/Repairing/Repaired, and fixing a mistake on its details.
    /// Same route prefix as TicketsController/AdminTicketsController — ASP.NET Core
    /// only requires each action's full route+verb to be unique, not the class name.
    /// </summary>
    [ApiController]
    [Route("api/tickets")]
    [Authorize]
    public class StaffTicketsController : ControllerBase
    {
        private readonly TenantCRMSDbContext _tenantDb;
        private readonly IEmailService _emailService;

        public StaffTicketsController(TenantCRMSDbContext tenantDb, IEmailService emailService)
        {
            _tenantDb = tenantDb;
            _emailService = emailService;
        }

        /// <summary>The logged-in Staff's own branch, if any — feeds the small
        /// branch pill in the header. Null/null when this account isn't
        /// assigned to a specific branch (most tenants).</summary>
        [HttpGet("~/api/staff/me")]
        public async Task<ActionResult<StaffProfileResponse>> GetMyProfile()
        {
            var staff = await FindCurrentStaffAsync();
            if (staff is null)
                return Ok(new StaffProfileResponse(null, null));

            if (staff.BranchId is null)
                return Ok(new StaffProfileResponse(null, null));

            string? branchName = await _tenantDb.Branches
                .Where(b => b.BranchId == staff.BranchId)
                .Select(b => b.Name)
                .FirstOrDefaultAsync();

            return Ok(new StaffProfileResponse(staff.BranchId, branchName));
        }

        /// <summary>Searches this tenant's customers by name/phone/email, so Staff can
        /// attach a new ticket to a returning customer instead of duplicating them.</summary>
        [HttpGet("~/api/customers/search")]
        public async Task<ActionResult<List<CustomerSearchResult>>> SearchCustomers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
                return Ok(new List<CustomerSearchResult>());

            string q = query.Trim();
            var results = await _tenantDb.Customers
                .Where(c => EF.Functions.Like(c.FullName, $"%{q}%")
                         || EF.Functions.Like(c.PhoneNumber, $"%{q}%")
                         || EF.Functions.Like(c.Email, $"%{q}%"))
                .Select(c => new CustomerSearchResult(c.CustomerId, c.FullName, c.PhoneNumber, c.Email, c.ServiceTickets.Count))
                .Take(20)
                .ToListAsync();

            return Ok(results);
        }

        [HttpPost]
        public async Task<ActionResult<TicketResponse>> CreateTicket(CreateTicketRequest request)
        {
            if (request.EstimatedCost <= 0)
                return BadRequest("Estimated cost must be greater than zero.");

            if (!request.ExistingCustomerId.HasValue)
            {
                if (!IsValidNameIfPresent(request.FirstName) || !IsValidNameIfPresent(request.MiddleName) || !IsValidNameIfPresent(request.LastName))
                    return BadRequest("Names can only contain letters — no numbers or symbols.");

                if (!IsValidPhoneIfPresent(request.Phone))
                    return BadRequest("Phone number can only contain digits — no letters.");
            }

            var staff = await FindCurrentStaffAsync();
            if (staff is null)
                return BadRequest("No Staff record found in this tenant for the logged-in account.");

            Customer customer;
            if (request.ExistingCustomerId.HasValue)
            {
                var existing = await _tenantDb.Customers.FindAsync(request.ExistingCustomerId.Value);
                if (existing is null)
                    return BadRequest($"No customer found with CustomerId {request.ExistingCustomerId.Value}.");
                customer = existing;
            }
            else
            {
                string fullName = string.Join(" ", new[] { request.FirstName, request.MiddleName, request.LastName }
                    .Where(p => !string.IsNullOrWhiteSpace(p)));

                customer = new Customer
                {
                    AdminId = staff.AdminId,
                    FullName = fullName,
                    PhoneNumber = request.Phone,
                    Email = request.Email ?? string.Empty
                };
                _tenantDb.Customers.Add(customer);
            }

            string? promotionTitle = null;
            if (request.PromotionId.HasValue)
            {
                var promotion = await _tenantDb.Promotions.FirstOrDefaultAsync(p => p.PromotionId == request.PromotionId.Value);
                if (promotion is null)
                    return BadRequest($"No promotion found with PromotionId {request.PromotionId.Value}.");
                if (promotion.Status != PromotionStatus.Approved)
                    return BadRequest("Only an approved promotion can be applied to a ticket.");
                promotionTitle = promotion.Title;
            }

            // A recorded "before" price only makes sense if it's actually more
            // than what the customer ends up paying — otherwise it's not a
            // discount, it's just a wrong number.
            if (request.OriginalPrice.HasValue && request.OriginalPrice.Value < request.EstimatedCost)
                return BadRequest("Original price can't be less than the final estimated cost.");

            var ticket = new ServiceTicket
            {
                Customer = customer,
                StaffId = staff.StaffId,
                BranchId = staff.BranchId,
                DeviceName = request.DeviceName,
                Concern = request.Concern,
                Status = "Diagnose",
                EstimatedCost = request.EstimatedCost,
                TermsAccepted = true,
                PromotionId = request.PromotionId,
                OriginalPrice = request.OriginalPrice
            };
            _tenantDb.ServiceTickets.Add(ticket);

            foreach (var part in request.SelectedParts ?? new List<SelectedPart>())
            {
                _tenantDb.RepairParts.Add(new RepairPart
                {
                    Ticket = ticket,
                    PartName = part.Name,
                    PartCost = part.Cost
                });
            }

            await _tenantDb.SaveChangesAsync();

            string? branchName = staff.BranchId.HasValue
                ? (await _tenantDb.Branches.FirstOrDefaultAsync(b => b.BranchId == staff.BranchId.Value))?.Name
                : null;

            await _emailService.SendAsync(customer.Email, customer.FullName,
                $"We've received your {ticket.DeviceName}",
                $"Hi {customer.FullName},\n\n" +
                $"We've received your {ticket.DeviceName} for repair (Ticket #{ticket.TicketId}).\n" +
                $"Reported concern: {ticket.Concern}\n\n" +
                "We'll keep you updated as work on it progresses.\n\n" +
                "— CRMS Repair Shop");

            return Ok(new TicketResponse(
                ticket.TicketId, customer.FullName, customer.PhoneNumber, customer.Email,
                ticket.DeviceName, ticket.Concern, ticket.EstimatedCost, ticket.Status,
                ticket.CreatedAt, ticket.CompletedAt, ticket.PickedUpAt, ticket.ArchivedAt, staff.Name,
                (request.SelectedParts ?? new List<SelectedPart>()),
                ticket.DiagnosisFindings, ticket.ApprovalStatus, ticket.ApprovedAmount, ticket.ApprovalDecisionAt, ticket.ApprovalMethod,
                ticket.PaymentStatus, ticket.AmountPaid, ticket.PromotionId, promotionTitle,
                ticket.OriginalPrice, ticket.OriginalPrice.HasValue ? ticket.OriginalPrice.Value - ticket.EstimatedCost : null,
                null, null,
                ticket.BranchId, branchName));
        }

        private static readonly string[] ValidStatuses = { "Diagnose", "AwaitingApproval", "Repairing", "Repaired", "Cancelled", "Unrepairable" };

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateStatusRequest request)
        {
            if (!ValidStatuses.Contains(request.Status))
                return BadRequest($"\"{request.Status}\" is not a recognized ticket status.");

            var ticket = await _tenantDb.ServiceTickets
                .Include(t => t.Customer)
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket is null)
                return NotFound();

            // The one real gate: paid work can't start until the customer approved
            // it. Enforced here, not just by hiding the option in the UI — checked
            // server-side so a raw API call can't skip it either.
            if (request.Status == "Repairing" && ticket.ApprovalStatus != "Approved")
                return BadRequest("This ticket can't move to Repairing until the customer has approved the diagnosis and estimate.");

            ticket.Status = request.Status;

            // CompletedAt doubles as "when this ticket stopped being active" for
            // every terminal outcome, not just a successful repair — avoids adding
            // a new column for Cancelled/Unrepairable while still recording when
            // each one happened.
            if (request.Status is "Repaired" or "Cancelled" or "Unrepairable")
                ticket.CompletedAt = DateTime.UtcNow;

            await _tenantDb.SaveChangesAsync();

            await _emailService.SendAsync(ticket.Customer.Email, ticket.Customer.FullName,
                $"Update on your {ticket.DeviceName}", BuildStatusChangeMessage(ticket));

            return Ok();
        }

        private static string BuildStatusChangeMessage(ServiceTicket ticket)
        {
            string body = ticket.Status switch
            {
                "AwaitingApproval" => $"we've diagnosed your {ticket.DeviceName} and it's now awaiting your approval on the repair estimate.",
                "Repairing" => $"your {ticket.DeviceName} is now being repaired.",
                "Repaired" => $"your {ticket.DeviceName} has been repaired and is ready for pickup!",
                "Cancelled" => $"the repair ticket for your {ticket.DeviceName} has been cancelled.",
                "Unrepairable" => $"unfortunately, your {ticket.DeviceName} could not be repaired.",
                _ => $"your {ticket.DeviceName}'s status is now: {ticket.Status}."
            };

            return $"Hi {ticket.Customer.FullName},\n\n{char.ToUpperInvariant(body[0])}{body[1..]}\n\n" +
                   $"Ticket #{ticket.TicketId}\n\n— CRMS Repair Shop";
        }

        /// <summary>Records diagnosis findings and, once the customer has responded,
        /// their approval decision — the one real gate before paid repair work can
        /// begin. Decision "Pending" just saves findings and moves the ticket to
        /// AwaitingApproval; "Approved" advances it to Repairing; "Rejected" closes
        /// it as Cancelled (same outcome Concern 3 already added).</summary>
        [HttpPut("{id}/approval")]
        public async Task<IActionResult> RecordApproval(int id, RecordApprovalRequest request)
        {
            if (request.Decision is not ("Pending" or "Approved" or "Rejected"))
                return BadRequest($"\"{request.Decision}\" is not a recognized approval decision.");

            if (request.EstimatedCost <= 0)
                return BadRequest("Estimated cost must be greater than zero.");

            if (request.Decision == "Approved" && request.ApprovedAmount is <= 0)
                return BadRequest("Approved amount must be greater than zero.");

            var ticket = await _tenantDb.ServiceTickets.Include(t => t.Customer).FirstOrDefaultAsync(t => t.TicketId == id);
            if (ticket is null)
                return NotFound();

            ticket.DiagnosisFindings = request.DiagnosisFindings;
            ticket.EstimatedCost = request.EstimatedCost;

            switch (request.Decision)
            {
                case "Pending":
                    ticket.ApprovalStatus = "Pending";
                    ticket.Status = "AwaitingApproval";
                    break;

                case "Approved":
                    ticket.ApprovalStatus = "Approved";
                    ticket.ApprovedAmount = request.ApprovedAmount ?? request.EstimatedCost;
                    ticket.ApprovalDecisionAt = DateTime.UtcNow;
                    ticket.ApprovalMethod = request.Method;
                    ticket.Status = "Repairing";
                    break;

                case "Rejected":
                    ticket.ApprovalStatus = "Rejected";
                    ticket.ApprovalDecisionAt = DateTime.UtcNow;
                    ticket.ApprovalMethod = request.Method;
                    ticket.Status = "Cancelled";
                    ticket.CompletedAt = DateTime.UtcNow;
                    break;
            }

            await _tenantDb.SaveChangesAsync();

            await _emailService.SendAsync(ticket.Customer.Email, ticket.Customer.FullName,
                $"Update on your {ticket.DeviceName}", BuildApprovalMessage(ticket, request.Decision));

            return Ok();
        }

        private static string BuildApprovalMessage(ServiceTicket ticket, string decision)
        {
            string body = decision switch
            {
                "Pending" =>
                    $"we've diagnosed your {ticket.DeviceName}.\n\n" +
                    $"Findings: {ticket.DiagnosisFindings}\n" +
                    $"Estimated cost: ₱{ticket.EstimatedCost:0.00}\n\n" +
                    "Please let us know if you'd like to go ahead with the repair.",
                "Approved" =>
                    $"thanks for approving the repair on your {ticket.DeviceName}.\n\n" +
                    $"Approved amount: ₱{ticket.ApprovedAmount:0.00}\n\n" +
                    "We're starting work on it now.",
                "Rejected" =>
                    $"we've noted your decision not to proceed with the repair on your {ticket.DeviceName}. " +
                    "The ticket has been cancelled — no charge will apply.",
                _ => $"your {ticket.DeviceName}'s status has been updated."
            };

            return $"Hi {ticket.Customer.FullName},\n\n{char.ToUpperInvariant(body[0])}{body[1..]}\n\n" +
                   $"Ticket #{ticket.TicketId}\n\n— CRMS Repair Shop";
        }

        /// <summary>Fixes a mistake in the customer's name/phone/email, or the
        /// device name, concern, or estimated cost, after a ticket already exists.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicketDetails(int id, UpdateTicketDetailsRequest request)
        {
            if (request.EstimatedCost <= 0)
                return BadRequest("Estimated cost must be greater than zero.");

            if (!IsValidNameIfPresent(request.FullName))
                return BadRequest("Name can only contain letters — no numbers or symbols.");

            if (!IsValidPhoneIfPresent(request.Phone))
                return BadRequest("Phone number can only contain digits — no letters.");

            var ticket = await _tenantDb.ServiceTickets
                .Include(t => t.Customer)
                .Include(t => t.RepairParts)
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket is null)
                return NotFound();

            ticket.Customer.FullName = request.FullName;
            ticket.Customer.PhoneNumber = request.Phone;
            ticket.Customer.Email = request.Email ?? string.Empty;

            if (request.OriginalPrice.HasValue && request.OriginalPrice.Value < request.EstimatedCost)
                return BadRequest("Original price can't be less than the final estimated cost.");

            ticket.DeviceName = request.DeviceName;
            ticket.Concern = request.Concern;
            ticket.EstimatedCost = request.EstimatedCost;

            if (request.ClearPromotion)
            {
                ticket.PromotionId = null;
                ticket.OriginalPrice = null;
            }
            else if (request.PromotionId.HasValue)
            {
                var promotion = await _tenantDb.Promotions.FirstOrDefaultAsync(p => p.PromotionId == request.PromotionId.Value);
                if (promotion is null)
                    return BadRequest($"No promotion found with PromotionId {request.PromotionId.Value}.");
                if (promotion.Status != PromotionStatus.Approved)
                    return BadRequest("Only an approved promotion can be applied to a ticket.");
                ticket.PromotionId = request.PromotionId;
                ticket.OriginalPrice = request.OriginalPrice;
            }

            // Replace the recorded parts with whatever's currently checked — the
            // chip selection in Edit always reflects the full, current set.
            if (request.SelectedParts is not null)
            {
                _tenantDb.RepairParts.RemoveRange(ticket.RepairParts);
                foreach (var part in request.SelectedParts)
                {
                    _tenantDb.RepairParts.Add(new RepairPart
                    {
                        TicketId = ticket.TicketId,
                        PartName = part.Name,
                        PartCost = part.Cost
                    });
                }
            }

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
