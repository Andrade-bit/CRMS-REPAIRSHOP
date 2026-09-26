using CRMS.api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRMS.api.Controllers
{
    public record SyncStatusResponse(string State, int PendingCount, DateTime? LastSyncedAt);

    /// <summary>The caller's own tenant's local-first sync status — read by the
    /// WinForms topbar indicator every 15-30s. Backed entirely by whatever
    /// TenantSyncBackgroundService last recorded; this endpoint never triggers
    /// a sync itself, it just reports the most recent known state.</summary>
    [ApiController]
    [Route("api/sync/status")]
    [Authorize]
    public class SyncStatusController : ControllerBase
    {
        private readonly ITenantSyncStatusStore _statusStore;

        public SyncStatusController(ITenantSyncStatusStore statusStore)
        {
            _statusStore = statusStore;
        }

        [HttpGet]
        public ActionResult<SyncStatusResponse> GetStatus()
        {
            string? companyIdClaim = User.FindFirst("CompanyId")?.Value;
            if (!int.TryParse(companyIdClaim, out int companyId))
                return Ok(new SyncStatusResponse("Unknown", 0, null));

            var snapshot = _statusStore.GetStatus(companyId);
            if (snapshot is null)
                return Ok(new SyncStatusResponse("Unknown", 0, null));

            return Ok(new SyncStatusResponse(snapshot.State.ToString(), snapshot.PendingCount, snapshot.LastSyncedAt));
        }
    }
}
