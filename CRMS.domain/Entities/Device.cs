using System;
using System.Collections.Generic;
using System.Text;

namespace CRMS.domain.Entities
{
    public class Device
    {
        public Guid DeviceId { get; set; }

        public int CompanyID { get; set; }

        public string DeviceCode { get; set; } = string.Empty;

        public string DeviceName { get; set; } = string.Empty;

        public bool IsaActive { get; set; }

        public DateTime CreatedAt { get; set; }

        //navigation property 
        public Company Company { get; set; } = null!;

    }
}
