using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS.domain.Entities
{
    public class Company : ISyncTracked
    {
        public int CompanyId { get; set; }

        public string CompanyCode { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty ;

        public bool IsActive { get; set; } = true;

        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        public ICollection<Device> Devices { get; set; } = new List<Device>();

        // ----- Local-first cloud sync ----- (see Staff.cs for the full explanation)
        public bool IsSynced { get; set; } = false;
        public DateTime? SyncedAt { get; set; }
        public int? CloudId { get; set; }
    }
}
