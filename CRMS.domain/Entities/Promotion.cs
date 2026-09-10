using System;

namespace CRMS.domain.Entities
{
    public class Promotion
    {
        public int PromotionId { get; set; }

        // FK -> Admin (lives in MasterDB — plain column only, no navigation)
        public int AdminId { get; set; }

        public string Message { get; set; } = string.Empty;
        public DateTime SentDate { get; set; } = DateTime.UtcNow;
    }
}