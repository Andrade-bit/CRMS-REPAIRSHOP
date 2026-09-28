using CRMS.domain.Entities;
using CRMS.infrastructure.Data;
using CRMS.infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRMS.api.Services
{
    /// <summary>
    /// The Master-database counterpart to TenantSyncBackgroundService: every
    /// 30 seconds, pushes whatever changed locally in Companies, Admins,
    /// Superadmins, TermsAgreements, and every login (ApplicationUser) to a
    /// single cloud copy of the Master database — the same "local is always
    /// the source of truth, cloud is never read back from" one-way model.
    ///
    /// Unlike tenant sync there's no per-company loop here — Master data
    /// belongs to the whole platform, not one tenant, so this is one pass
    /// against one fixed cloud target (ConnectionStrings:MasterCloud).
    ///
    /// Push order matters: Companies first (ApplicationUser.CompanyId points
    /// at one), then ApplicationUsers and their role assignments (Superadmin/
    /// Admin both point at one by Id), then Superadmins, then Admins, then
    /// TermsAgreements (points at a Superadmin).
    /// </summary>
    public class MasterSyncBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMasterSyncStatusStore _statusStore;
        private readonly ILogger<MasterSyncBackgroundService> _logger;

        public MasterSyncBackgroundService(
            IServiceScopeFactory scopeFactory, IMasterSyncStatusStore statusStore, ILogger<MasterSyncBackgroundService> logger)
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
                    await SyncOnceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Master sync pass failed unexpectedly.");
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

        private async Task SyncOnceAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var connectionService = scope.ServiceProvider.GetRequiredService<ITenantConnectionService>();

            string? cloudConnectionString = connectionService.GetMasterCloudConnectionString();
            if (string.IsNullOrWhiteSpace(cloudConnectionString))
                return; // no cloud target configured — nothing to sync to yet

            var localDb = scope.ServiceProvider.GetRequiredService<MasterCRMSDbContext>();

            int pendingBefore;
            try
            {
                pendingBefore = await CountPendingAsync(localDb, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not reach the local Master database this pass.");
                return;
            }

            var cloudOptions = new DbContextOptionsBuilder<MasterCRMSDbContext>().UseSqlServer(cloudConnectionString).Options;
            await using var cloudDb = new MasterCRMSDbContext(cloudOptions);

            // The cloud context is only ever a write destination here, never
            // something read back for "what's still pending" — without this,
            // its own auto-dirty-tracking would silently reset IsSynced back
            // to false on every new cloud row right before the insert,
            // immediately after this code sets it to true below.
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
                _statusStore.SetStatus(new TenantSyncStatusSnapshot(TenantSyncState.Offline, pendingBefore, null));
                return;
            }

            try
            {
                await PushCompaniesAsync(localDb, cloudDb, ct);
                await PushUsersAsync(localDb, cloudDb, ct);
                await PushSuperadminsAsync(localDb, cloudDb, ct);
                await PushAdminsAsync(localDb, cloudDb, ct);
                await PushTermsAgreementsAsync(localDb, cloudDb, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Master sync push failed partway through.");
            }

            int pendingAfter = await CountPendingAsync(localDb, ct);
            _statusStore.SetStatus(new TenantSyncStatusSnapshot(TenantSyncState.Online, pendingAfter, DateTime.UtcNow));
        }

        private static async Task<int> CountPendingAsync(MasterCRMSDbContext db, CancellationToken ct)
        {
            int companies = await db.Companies.CountAsync(c => !c.IsSynced, ct);
            int users = await db.Users.CountAsync(u => !u.IsSynced, ct);
            int superadmins = await db.Superadmins.CountAsync(s => !s.IsSynced, ct);
            int admins = await db.Admins.CountAsync(a => !a.IsSynced, ct);
            int terms = await db.TermsAgreements.CountAsync(t => !t.IsSynced, ct);
            return companies + users + superadmins + admins + terms;
        }

        private static async Task SaveLocalSyncStateAsync(MasterCRMSDbContext local, CancellationToken ct)
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

        private static async Task PushCompaniesAsync(MasterCRMSDbContext local, MasterCRMSDbContext cloud, CancellationToken ct)
        {
            var pending = await local.Companies.Where(c => !c.IsSynced).ToListAsync(ct);
            if (pending.Count == 0) return;

            foreach (var row in pending)
            {
                Company? cloudRow = row.CloudId.HasValue
                    ? await cloud.Companies.FirstOrDefaultAsync(c => c.CompanyId == row.CloudId.Value, ct)
                    : null;

                bool isNew = cloudRow is null;
                cloudRow ??= new Company();

                cloudRow.CompanyCode = row.CompanyCode;
                cloudRow.CompanyName = row.CompanyName;
                cloudRow.IsActive = row.IsActive;
                cloudRow.CreateAt = row.CreateAt;
                cloudRow.IsSynced = true;
                cloudRow.SyncedAt = DateTime.UtcNow;

                if (isNew) cloud.Companies.Add(cloudRow);
                await cloud.SaveChangesAsync(ct);

                row.CloudId = cloudRow.CompanyId;
                row.IsSynced = true;
                row.SyncedAt = DateTime.UtcNow;
            }

            await SaveLocalSyncStateAsync(local, ct);
        }

        /// <summary>Every login (Superadmin, every tenant's Admin/Manager/
        /// Staff), plus their role assignments. The cloud row reuses the
        /// exact same Id as the local one — Identity's Id is already a
        /// globally-unique string, so no CloudId mapping is needed, and every
        /// FK that points at an ApplicationUserId just works unchanged.</summary>
        private static async Task PushUsersAsync(MasterCRMSDbContext local, MasterCRMSDbContext cloud, CancellationToken ct)
        {
            var pending = await local.Users.Where(u => !u.IsSynced).ToListAsync(ct);
            if (pending.Count == 0) return;

            foreach (var row in pending)
            {
                // A tenant-scoped user must wait for its Company to sync first
                // (its CloudId is what the cloud user row needs) — retried
                // automatically next pass once that Company row syncs.
                int? cloudCompanyId = null;
                if (row.CompanyId.HasValue)
                {
                    var company = await local.Companies.FirstOrDefaultAsync(c => c.CompanyId == row.CompanyId.Value, ct);
                    if (company?.CloudId is null) continue;
                    cloudCompanyId = company.CloudId;
                }

                var cloudRow = await cloud.Users.FirstOrDefaultAsync(u => u.Id == row.Id, ct);
                bool isNew = cloudRow is null;
                cloudRow ??= new ApplicationUser { Id = row.Id };

                cloudRow.UserName = row.UserName;
                cloudRow.NormalizedUserName = row.NormalizedUserName;
                cloudRow.Email = row.Email;
                cloudRow.NormalizedEmail = row.NormalizedEmail;
                cloudRow.EmailConfirmed = row.EmailConfirmed;
                cloudRow.PasswordHash = row.PasswordHash;
                cloudRow.SecurityStamp = row.SecurityStamp;
                cloudRow.ConcurrencyStamp = row.ConcurrencyStamp;
                cloudRow.PhoneNumber = row.PhoneNumber;
                cloudRow.PhoneNumberConfirmed = row.PhoneNumberConfirmed;
                cloudRow.TwoFactorEnabled = row.TwoFactorEnabled;
                cloudRow.LockoutEnd = row.LockoutEnd;
                cloudRow.LockoutEnabled = row.LockoutEnabled;
                cloudRow.AccessFailedCount = row.AccessFailedCount;
                cloudRow.FullName = row.FullName;
                cloudRow.CompanyId = cloudCompanyId;
                cloudRow.Status = row.Status;
                cloudRow.IsSynced = true;
                cloudRow.SyncedAt = DateTime.UtcNow;

                if (isNew) cloud.Users.Add(cloudRow);
                await cloud.SaveChangesAsync(ct);

                await PushUserRolesAsync(local, cloud, row.Id, ct);

                row.IsSynced = true;
                row.SyncedAt = DateTime.UtcNow;
            }

            await SaveLocalSyncStateAsync(local, ct);
        }

        /// <summary>Role assignments are matched by role NAME, not RoleId —
        /// each database seeds its own Roles table independently at startup
        /// (see Program.cs), so the same role ("Admin", "Staff", ...) has a
        /// different Id locally than it does in the cloud database.</summary>
        private static async Task PushUserRolesAsync(MasterCRMSDbContext local, MasterCRMSDbContext cloud, string userId, CancellationToken ct)
        {
            var localRoleNames = await (
                from ur in local.UserRoles
                join r in local.Roles on ur.RoleId equals r.Id
                where ur.UserId == userId
                select r.Name).ToListAsync(ct);

            foreach (var roleName in localRoleNames)
            {
                var cloudRole = await cloud.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
                if (cloudRole is null) continue; // this role hasn't been seeded in the cloud DB yet

                bool alreadyAssigned = await cloud.UserRoles
                    .AnyAsync(ur => ur.UserId == userId && ur.RoleId == cloudRole.Id, ct);
                if (alreadyAssigned) continue;

                cloud.UserRoles.Add(new IdentityUserRole<string> { UserId = userId, RoleId = cloudRole.Id });
            }

            await cloud.SaveChangesAsync(ct);
        }

        private static async Task PushSuperadminsAsync(MasterCRMSDbContext local, MasterCRMSDbContext cloud, CancellationToken ct)
        {
            var pending = await local.Superadmins.Where(s => !s.IsSynced).ToListAsync(ct);
            if (pending.Count == 0) return;

            foreach (var row in pending)
            {
                // Its ApplicationUser must already be synced — same Id is
                // reused, so this just confirms the cloud row exists yet.
                bool userSynced = await cloud.Users.AnyAsync(u => u.Id == row.ApplicationUserId, ct);
                if (!userSynced) continue;

                Superadmin? cloudRow = row.CloudId.HasValue
                    ? await cloud.Superadmins.FirstOrDefaultAsync(s => s.SuperadminId == row.CloudId.Value, ct)
                    : null;

                bool isNew = cloudRow is null;
                cloudRow ??= new Superadmin();

                cloudRow.ApplicationUserId = row.ApplicationUserId;
                cloudRow.IsSynced = true;
                cloudRow.SyncedAt = DateTime.UtcNow;

                if (isNew) cloud.Superadmins.Add(cloudRow);
                await cloud.SaveChangesAsync(ct);

                row.CloudId = cloudRow.SuperadminId;
                row.IsSynced = true;
                row.SyncedAt = DateTime.UtcNow;
            }

            await SaveLocalSyncStateAsync(local, ct);
        }

        private static async Task PushAdminsAsync(MasterCRMSDbContext local, MasterCRMSDbContext cloud, CancellationToken ct)
        {
            var pending = await local.Admins.Where(a => !a.IsSynced).ToListAsync(ct);
            if (pending.Count == 0) return;

            foreach (var row in pending)
            {
                bool userSynced = await cloud.Users.AnyAsync(u => u.Id == row.ApplicationUserId, ct);
                if (!userSynced) continue; // retried automatically once that login syncs

                int? cloudSuperadminId = null;
                if (row.SuperadminId.HasValue)
                {
                    var superadmin = await local.Superadmins.FirstOrDefaultAsync(s => s.SuperadminId == row.SuperadminId.Value, ct);
                    if (superadmin?.CloudId is null) continue; // retried automatically once the superadmin syncs
                    cloudSuperadminId = superadmin.CloudId;
                }

                Admin? cloudRow = row.CloudId.HasValue
                    ? await cloud.Admins.FirstOrDefaultAsync(a => a.AdminId == row.CloudId.Value, ct)
                    : null;

                bool isNew = cloudRow is null;
                cloudRow ??= new Admin();

                cloudRow.ApplicationUserId = row.ApplicationUserId;
                cloudRow.SuperadminId = cloudSuperadminId;
                cloudRow.Name = row.Name;
                cloudRow.ShopName = row.ShopName;
                cloudRow.Address = row.Address;
                cloudRow.SubscriptionStatus = row.SubscriptionStatus;
                cloudRow.BillingCycle = row.BillingCycle;
                cloudRow.NextBillingDate = row.NextBillingDate;
                cloudRow.SubscriptionAmount = row.SubscriptionAmount;
                cloudRow.SubscriptionPlan = row.SubscriptionPlan;
                cloudRow.IsSynced = true;
                cloudRow.SyncedAt = DateTime.UtcNow;

                if (isNew) cloud.Admins.Add(cloudRow);
                await cloud.SaveChangesAsync(ct);

                row.CloudId = cloudRow.AdminId;
                row.IsSynced = true;
                row.SyncedAt = DateTime.UtcNow;
            }

            await SaveLocalSyncStateAsync(local, ct);
        }

        private static async Task PushTermsAgreementsAsync(MasterCRMSDbContext local, MasterCRMSDbContext cloud, CancellationToken ct)
        {
            var pending = await local.TermsAgreements.Where(t => !t.IsSynced).ToListAsync(ct);
            if (pending.Count == 0) return;

            foreach (var row in pending)
            {
                var superadmin = await local.Superadmins.FirstOrDefaultAsync(s => s.SuperadminId == row.SuperadminId, ct);
                if (superadmin?.CloudId is null) continue; // retried automatically once the superadmin syncs

                TermsAgreement? cloudRow = row.CloudId.HasValue
                    ? await cloud.TermsAgreements.FirstOrDefaultAsync(t => t.TermsId == row.CloudId.Value, ct)
                    : null;

                bool isNew = cloudRow is null;
                cloudRow ??= new TermsAgreement();

                cloudRow.SuperadminId = superadmin.CloudId.Value;
                cloudRow.Version = row.Version;
                cloudRow.Content = row.Content;
                cloudRow.CreatedAt = row.CreatedAt;
                cloudRow.IsCurrent = row.IsCurrent;
                cloudRow.PlanScope = row.PlanScope;
                cloudRow.IsSynced = true;
                cloudRow.SyncedAt = DateTime.UtcNow;

                if (isNew) cloud.TermsAgreements.Add(cloudRow);
                await cloud.SaveChangesAsync(ct);

                row.CloudId = cloudRow.TermsId;
                row.IsSynced = true;
                row.SyncedAt = DateTime.UtcNow;
            }

            await SaveLocalSyncStateAsync(local, ct);
        }
    }
}
