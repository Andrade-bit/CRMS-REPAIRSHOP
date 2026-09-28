using System.Collections.Concurrent;

namespace CRMS.api.Services
{
    public enum TenantSyncState { Unknown, Online, Offline }

    public record TenantSyncStatusSnapshot(TenantSyncState State, int PendingCount, DateTime? LastSyncedAt);

    /// <summary>In-memory record of each tenant's last-known sync state, written
    /// by TenantSyncBackgroundService and read by the /api/sync/status endpoint.
    /// Deliberately not persisted — it's a live status display, not a record
    /// that needs to survive an API restart.</summary>
    public interface ITenantSyncStatusStore
    {
        void SetStatus(int companyId, TenantSyncStatusSnapshot snapshot);
        TenantSyncStatusSnapshot? GetStatus(int companyId);
    }

    public class TenantSyncStatusStore : ITenantSyncStatusStore
    {
        private readonly ConcurrentDictionary<int, TenantSyncStatusSnapshot> _statuses = new();

        public void SetStatus(int companyId, TenantSyncStatusSnapshot snapshot) => _statuses[companyId] = snapshot;

        public TenantSyncStatusSnapshot? GetStatus(int companyId) =>
            _statuses.TryGetValue(companyId, out var snapshot) ? snapshot : null;
    }

    /// <summary>Same idea as ITenantSyncStatusStore, but there's only ever one
    /// Master database (not one per tenant), so this just holds a single
    /// snapshot instead of a dictionary keyed by CompanyId.</summary>
    public interface IMasterSyncStatusStore
    {
        void SetStatus(TenantSyncStatusSnapshot snapshot);
        TenantSyncStatusSnapshot? GetStatus();
    }

    public class MasterSyncStatusStore : IMasterSyncStatusStore
    {
        private TenantSyncStatusSnapshot? _status;

        public void SetStatus(TenantSyncStatusSnapshot snapshot) => _status = snapshot;

        public TenantSyncStatusSnapshot? GetStatus() => _status;
    }
}
