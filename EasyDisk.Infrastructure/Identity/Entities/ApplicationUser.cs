using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Identity.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public long UsedQuotaBytes { get; set; } = 0;
        public long MaxStorageBytes { get; set; } = 21474836480;

        public string SubscriptionPlan { get; set; } = "Free";
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        public DateTime? BirthDate { get; set; }

        public DateTimeOffset? BannedAt { get; set; }
        public bool IsStorageWiped { get; set; } = false;
    }
}
