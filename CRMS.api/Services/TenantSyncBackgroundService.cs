using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using CRMS.infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Services
{
    /// <summary>
    /// The local-first sync worker: every 30 seconds, for every active tenant,
    /// checks whether that tenant's cloud database is reachable and — if so —
    /// pushes whatever's changed locally since the last pass. Push order
    /// matters: Staff and Customers first (ServiceTickets reference them),
    /// then Promotions, then ServiceTickets and their child rows last.
    ///
    /// One direction only: local is always the source of truth, the cloud
    /// database is never read back from except to confirm a row already
    /// exists there. That's what makes this safe without any conflict
    /// resolution — nothing else ever writes to a tenant's cloud database.
    /// </summary>
    public class TenantSyncBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantSyncStatusStore _statusStore;
        private readonly ILogger<TenantSyncBackgroundService> _logger;

        public TenantSyncBackgroundService(
            IServiceScopeFactory scopeFactory, ITenantSyncStatusStore statusStore, ILogger<TenantSyncBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _statusStore = statusStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncAllTenantsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Tenant sync pass failed unexpectedly.");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Normal on shutdown.
                }
            }
        }

        private async Task SyncAllTenantsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var masterDb = scope.ServiceProvider.GetRequiredService<MasterCRMSDbContext>();
            var connectionService = scope.ServiceProvider.GetRequiredService<ITenantConnectionService>();

            var companies = await masterDb.Companies.Where(c => c.IsActive).ToListAsync(ct);
            foreach (var company in companies)
            {
                await SyncTenantAsync(company.CompanyId, connectionService, ct);
            }
        }

        private async Task SyncTenantAsync(int companyId, ITenantConnectionService connectionService, CancellationToken ct)
        {
            string localConnectionString;
            string cloudConnectionString;
            try
            {
                localConnectionString = await connectionService.GetTenantConnectionStringAsync(companyId);
                cloudConnectionString = await connectionService.GetTenantCloudConnectionStringAsync(companyId);
            }
            catch (InvalidOperationException)
            {
                // No cloud credentials configured for this tenant — nothing to sync to yet.
                return;
            }

            var localOptions = new DbContextOptionsBuilder<TenantCRMSDbContext>().UseSqlServer(localConnectionString).Options;
            await using var localDb = new TenantCRMSDbContext(localOptions);

            int pendingBefore;
            try
            {
                pendingBefore = await CountPendingAsync(localDb, ct);
            }
            catch (Exception ex)
            {
                // The local database itself is unreachable — nothing this
                // background pass can do; the app's own normal requests will
                // already be failing too, so there's nothing new to report.
                _logger.LogWarning(ex, "Could not reach the local database for CompanyId {CompanyId} this pass.", companyId);
                return;
            }

            var cloudOptions = new DbContextOptionsBuilder<TenantCRMSDbContext>().UseSqlServer(cloudConnectionString).Options;
            await using var cloudDb = new TenantCRMSDbContext(cloudOptions);

            // The cloud context is only ever a write destination here, never
            // something read back for "what's still pending" — without this,
            // its own auto-dirty-tracking would silently reset IsSynced back
            // to false on every new/edited cloud row right before the save,
            // immediately after each Push*Async method sets it to true.
            cloudDb.SuppressSyncDirtyTracking = true;

            bool cloudReachable;
            try
            {
                cloudReachable = await cloudDb.Database.CanConnectAsync(ct);
            }
            catch
            {
                cloudReachable = false;
            }

            if (!cloudReachable)
            {
                _statusStore.SetStatus(companyId, new TenantSyncStatusSnapshot(TenantSyncState.Offline, pendingBefore, null));
                return;
            }

            try
            {
                await PushBranchesAsync(localDb, cloudDb, ct);
                await PushStaffAsync(localDb, cloudDb, ct);
                await PushCustomersAsync(localDb, cloudDb, ct);
                await PushPromotionsAsync(localDb, cloudDb, ct);
                await PushTicketsAsync(localDb, cloudDb, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync push failed partway through for CompanyId {CompanyId}.", companyId);
            }

            int pendingAfter = await CountPendingAsync(localDb, ct);
            _statusStore.SetStatus(companyId, new TenantSyncStatusSnapshot(TenantSyncState.Online, pendingAfter, DateTime.UtcNow));
        }

        private static async Task<int> CountPendingAsync(TenantCRMSDbContext db, CancellationToken ct)
        {
            int staff = await db.Staff.CountAsync(s => !s.IsSynced, ct);
            int customers = await db.Customers.CountAsync(c => !c.IsSynced, ct);
            int promotions = await db.Promotions.CountAsync(p => !p.IsSynced, ct);
            int tickets = await db.ServiceTickets.CountAsync(t => !t.IsSynced, ct);

            // Most tenants' databases were never migrated to include Branches at
            // all (see BranchController) — that's not a sync failure, just
            // nothing to count here.
            int branches = 0;
            try
            {
                branches = await db.Branches.CountAsync(b => !b.IsSynced, ct);
            }
            catch (SqlException)
            {
            }

            return staff + customers + promotions + tickets + branches;
        }

        private static async Task SaveLocalSyncStateAsync(TenantCRMSDbContext local, CancellationToken ct)
        {
            local.SuppressSyncDirtyTracking = true;
            try
            {
                await local.SaveChangesAsync(ct);
            }
            finally
            {
                local.SuppressSyncDirtyTracking = false;
            }
        }

        /// <summary>Most tenants' databases were never migrated to include
        /// Branches at all — a missing table there is normal, not an error, so
        /// this returns quietly instead of aborting the rest of the pass.</summary>
        private static async Task PushBranchesAsync(TenantCRMSDbContext local, TenantCRMSDbContext cloud, CancellationToken ct)
        {
            List<Branch> pending;
            try
            {
                pending = await local.Branches.Where(b => !b.IsSynced).ToListAsync(ct);
            }
            catch (SqlException)
            {
                return;
            }
            if (pending.Count == 0) return;

            foreach (var row in pending)
            {
                Branch? cloudRow = row.CloudId.HasValue
                    ? await cloud.Branches.FirstOrDefaultAsync(b => b.BranchId == row.CloudId.Value, ct)
                    : null;

                bool isNew = cloudRow is null;
                cloudRow ??= new Branch();

                cloudRow.Name = row.Name;
                cloudRow.Address = row.Address;
                cloudRow.Phone = row.Phone;
                cloudRow.Email = row.Email;
                cloudRow.CreatedAt = row.CreatedAt;
                cloudRow.IsSynced = true;
                cloudRow.SyncedAt = DateTime.UtcNow;

                if (isNew) cloud.Branches.Add(cloudRow);
                await cloud.SaveChangesAsync(ct);

                row.CloudId = cloudRow.BranchId;
                row.IsSynced = true;
                row.SyncedAt = DateTime.UtcNow;
            }

            await SaveLocalSyncStateAsync(local, ct);
        }

        private static async Task PushStaffAsync(TenantCRMSDbContext local, TenantCRMSDbContext cloud, CancellationToken ct)
        {
            var pending = await local.Staff.Where(s => !s.IsSynced).ToListAsync(ct);
            if (pending.Count == 0) return;

            foreach (var row in pending)
            {
                // A branch-assigned Staff row must wait for its Branch to sync
                // first (its CloudId is what the cloud Staff row needs) — retried
                // automatically next pass once that Branch row syncs.
                int? cloudBranchId = null;
                if (row.BranchId.HasValue)
                {
                    var branch = await local.Branches.FirstOrDefaultAsync(b => b.BranchId == row.BranchId.Value, ct);
                    if (branch?.CloudId is null) continue;
                    cloudBranchId = branch.CloudId;
                }

                Staff? cloudRow = row.CloudId.HasValue
                    ? await cloud.Staff.FirstOrDefaultAsync(s => s.StaffId == row.CloudId.Value, ct)
                    : null;

                bool isNew = cloudRow is null;
                cloudRow ??= new Staff();

                cloudRow.ApplicationUserId = row.ApplicationUserId;
                cloudRow.AdminId = row.AdminId;
                cloudRow.Name = row.Name;
                cloudRow.Status = row.Status;
                cloudRow.BranchId = cloudBranchId;
                cloudRow.IsSynced = true;
                cloudRow.SyncedAt = DateTime.UtcNow;

                if (isNew) cloud.Staff.Add(cloudRow);
                await cloud.SaveChangesAsync(ct);

                row.CloudId = cloudRow.StaffId;
                row.IsSynced = true;
                row.SyncedAt = DateTime.UtcNow;
            }

            await SaveLocalSyncStateAsync(local, ct);
        }

        private static async Task PushCustomersAsync(TenantCRMSDbContext local, TenantCRMSDbContext cloud, CancellationToken ct)
        {
            var pending = await local.Customers.Where(c => !c.IsSynced).ToListAsync(ct);
            if (pending.Count == 0) return;

            foreach (var row in pending)
            {
                Customer? cloudRow = row.CloudId.HasValue
                    ? await cloud.Customers.FirstOrDefaultAsync(c => c.CustomerId == row.CloudId.Value, ct)
                    : null;

                bool isNew = cloudRow is null;
                cloudRow ??= new Customer();

                cloudRow.AdminId = row.AdminId;
                cloudRow.FullName = row.FullName;
                cloudRow.PhoneNumber = row.PhoneNumber;
                cloudRow.Email = row.Email;
                cloudRow.IsSynced = true;
                cloudRow.SyncedAt = DateTime.UtcNow;

                if (isNew) cloud.Customers.Add(cloudRow);
                await cloud.SaveChangesAsync(ct);

                row.CloudId = cloudRow.CustomerId;
                row.IsSynced = true;
                row.SyncedAt = DateTime.UtcNow;
            }

            await SaveLocalSyncStateAsync(local, ct);
        }

        private static async Task PushPromotionsAsync(TenantCRMSDbContext local, TenantCRMSDbContext cloud, CancellationToken ct)
        {
            var pending = await local.Promotions.Where(p => !p.IsSynced).ToListAsync(ct);
            if (pending.Count == 0) return;

            foreach (var row in pending)
            {
                // A promotion's Staff must already be synced — if it isn't yet
                // (StaffId's CloudId is null), skip it this round; it'll be
                // picked up automatically once that Staff row syncs first.
                var staff = await local.Staff.FirstOrDefaultAsync(s => s.StaffId == row.StaffId, ct);
                if (staff?.CloudId is null) continue;

                Promotion? cloudRow = row.CloudId.HasValue
                    ? await cloud.Promotions.FirstOrDefaultAsync(p => p.PromotionId == row.CloudId.Value, ct)
                    : null;

                bool isNew = cloudRow is null;
                cloudRow ??= new Promotion();

                cloudRow.StaffId = staff.CloudId.Value;
                cloudRow.ReviewedByAdminId = row.ReviewedByAdminId;
                cloudRow.Title = row.Title;
                cloudRow.Type = row.Type;
                cloudRow.Message = row.Message;
                cloudRow.Reason = row.Reason;
                cloudRow.Status = row.Status;
                cloudRow.CreatedAt = row.CreatedAt;
                cloudRow.ReviewedAt = row.ReviewedAt;
                cloudRow.IsSynced = true;
                cloudRow.SyncedAt = DateTime.UtcNow;

                if (isNew) cloud.Promotions.Add(cloudRow);
                await cloud.SaveChangesAsync(ct);

                row.CloudId = cloudRow.PromotionId;
                row.IsSynced = true;
                row.SyncedAt = DateTime.UtcNow;
            }

            await SaveLocalSyncStateAsync(local, ct);
        }

        private static async Task PushTicketsAsync(TenantCRMSDbContext local, TenantCRMSDbContext cloud, CancellationToken ct)
        {
            var pending = await local.ServiceTickets
                .Include(t => t.RepairParts)
                .Include(t => t.Payments)
                .Include(t => t.Feedbacks)
                .Where(t => !t.IsSynced)
                .ToListAsync(ct);
            if (pending.Count == 0) return;

            foreach (var row in pending)
            {
                var customer = await local.Customers.FirstOrDefaultAsync(c => c.CustomerId == row.CustomerId, ct);
                if (customer?.CloudId is null) continue; // retried automatically once the customer syncs

                int? cloudStaffId = null;
                if (row.StaffId.HasValue)
                {
                    var staff = await local.Staff.FirstOrDefaultAsync(s => s.StaffId == row.StaffId.Value, ct);
                    if (staff?.CloudId is null) continue; // retried automatically once the staff row syncs
                    cloudStaffId = staff.CloudId;
                }

                int? cloudPromotionId = null;
                if (row.PromotionId.HasValue)
                {
                    var promotion = await local.Promotions.FirstOrDefaultAsync(p => p.PromotionId == row.PromotionId.Value, ct);
                    if (promotion?.CloudId is null) continue; // retried automatically once the promotion syncs
                    cloudPromotionId = promotion.CloudId;
                }

                int? cloudBranchId = null;
                if (row.BranchId.HasValue)
                {
                    var branch = await local.Branches.FirstOrDefaultAsync(b => b.BranchId == row.BranchId.Value, ct);
                    if (branch?.CloudId is null) continue; // retried automatically once the branch syncs
                    cloudBranchId = branch.CloudId;
                }

                ServiceTicket? cloudRow = row.CloudId.HasValue
                    ? await cloud.ServiceTickets
                        .Include(t => t.RepairParts).Include(t => t.Payments).Include(t => t.Feedbacks)
                        .FirstOrDefaultAsync(t => t.TicketId == row.CloudId.Value, ct)
                    : null;

                bool isNew = cloudRow is null;
                cloudRow ??= new ServiceTicket();

                cloudRow.CustomerId = customer.CloudId.Value;
                cloudRow.StaffId = cloudStaffId;
                cloudRow.BranchId = cloudBranchId;
                cloudRow.DeviceName = row.DeviceName;
                cloudRow.Concern = row.Concern;
                cloudRow.Status = row.Status;
                cloudRow.EstimatedCost = row.EstimatedCost;
                cloudRow.PaymentStatus = row.PaymentStatus;
                cloudRow.AmountPaid = row.AmountPaid;
                cloudRow.CreatedAt = row.CreatedAt;
                cloudRow.CompletedAt = row.CompletedAt;
                cloudRow.PickedUpAt = row.PickedUpAt;
                cloudRow.ArchivedAt = row.ArchivedAt;
                cloudRow.TermsAccepted = row.TermsAccepted;
                cloudRow.DiagnosisFindings = row.DiagnosisFindings;
                cloudRow.ApprovalStatus = row.ApprovalStatus;
                cloudRow.ApprovedAmount = row.ApprovedAmount;
                cloudRow.ApprovalDecisionAt = row.ApprovalDecisionAt;
                cloudRow.ApprovalMethod = row.ApprovalMethod;
                cloudRow.PromotionId = cloudPromotionId;
                cloudRow.OriginalPrice = row.OriginalPrice;
                cloudRow.IsSynced = true;
                cloudRow.SyncedAt = DateTime.UtcNow;

                // Children are small in number per ticket — replaced wholesale
                // on every sync rather than tracked individually, since that
                // needs no sync-state columns of their own and stays correct
                // however many times a ticket's parts/payments/feedback change.
                // Explicitly removed via the DbSet (not just Collection.Clear())
                // because each child's FK to its ticket is required — merely
                // clearing the navigation leaves EF unable to decide whether to
                // delete or orphan them and it throws instead.
                cloud.RepairParts.RemoveRange(cloudRow.RepairParts.ToList());
                foreach (var part in row.RepairParts)
                    cloudRow.RepairParts.Add(new RepairPart { PartName = part.PartName, PartCost = part.PartCost });

                cloud.Payments.RemoveRange(cloudRow.Payments.ToList());
                foreach (var payment in row.Payments)
                    cloudRow.Payments.Add(new Payment { Amount = payment.Amount, PaymentType = payment.PaymentType, PaymentDate = payment.PaymentDate });

                cloud.Feedbacks.RemoveRange(cloudRow.Feedbacks.ToList());
                foreach (var feedback in row.Feedbacks)
                    cloudRow.Feedbacks.Add(new Feedback { Rating = feedback.Rating, Message = feedback.Message, IsRead = feedback.IsRead, CreatedAt = feedback.CreatedAt });

                if (isNew) cloud.ServiceTickets.Add(cloudRow);
                await cloud.SaveChangesAsync(ct);

                row.CloudId = cloudRow.TicketId;
                row.IsSynced = true;
                row.SyncedAt = DateTime.UtcNow;
            }

            await SaveLocalSyncStateAsync(local, ct);
        }
    }
}
