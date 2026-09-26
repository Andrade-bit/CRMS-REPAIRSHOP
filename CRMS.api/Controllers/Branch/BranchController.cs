using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Controllers
{
    /// <summary>
    /// A tenant's own branches — a tenant can have any number of them. Whether
    /// this tenant has Branch Management at all is what the WinForms Admin
    /// client uses to decide whether to show the "Branch Management" nav item:
    /// a tenant whose database was never migrated to include Branches gets a
    /// 404 from GetBranches, and never sees the page — a tenant that has the
    /// table but zero rows yet still gets 200 with an empty list, so Admin can
    /// still get to the page to add their first branch.
    ///
    /// Branch Management is deliberately only applied to select tenants: the
    /// migration that creates the Branches table is applied by hand to
    /// whichever tenant's own database should have it, not to every tenant
    /// like every other migration so far — so a tenant without it genuinely
    /// has no such table at all (not just zero rows in it). GetBranches has to
    /// tolerate that missing-table case explicitly, or an Admin whose tenant
    /// lacks it would get a 500 on the unconditional startup check for
    /// whether Branch Management should show.
    /// </summary>
    [ApiController]
    [Route("api/branch")]
    [Authorize(Roles = "Admin")]
    public class BranchController : ControllerBase
    {
        private readonly TenantCRMSDbContext _tenantDb;

        public BranchController(TenantCRMSDbContext tenantDb)
        {
            _tenantDb = tenantDb;
        }

        [HttpGet]
        public async Task<ActionResult<List<BranchResponse>>> GetBranches()
        {
            List<Branch> branches;
            try
            {
                branches = await _tenantDb.Branches.OrderBy(b => b.Name).ToListAsync();
            }
            catch (SqlException)
            {
                // This tenant's database was never migrated to include
                // Branches at all — same as "no Branch Management" from the
                // caller's perspective.
                return NotFound();
            }

            return Ok(branches.Select(ToResponse).ToList());
        }

        [HttpPost]
        public async Task<ActionResult<BranchResponse>> CreateBranch(CreateBranchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Branch name is required.");

            var branch = new Branch
            {
                Name = request.Name,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email
            };
            _tenantDb.Branches.Add(branch);
            await _tenantDb.SaveChangesAsync();

            return Ok(ToResponse(branch));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBranch(int id, UpdateBranchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Branch name is required.");

            var branch = await _tenantDb.Branches.FirstOrDefaultAsync(b => b.BranchId == id);
            if (branch is null) return NotFound("No branch found with that id.");

            branch.Name = request.Name;
            branch.Address = request.Address;
            branch.Phone = request.Phone;
            branch.Email = request.Email;
            await _tenantDb.SaveChangesAsync();

            return Ok(ToResponse(branch));
        }

        private static BranchResponse ToResponse(Branch branch) =>
            new(branch.BranchId, branch.Name, branch.Address, branch.Phone, branch.Email);
    }
}
