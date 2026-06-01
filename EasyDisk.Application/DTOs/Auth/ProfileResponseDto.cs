using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs
{
    public class ProfileResponseDto
    {
        public string? Username { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public DateTime? BirthDate { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public long UsedQuotaBytes { get; set; }
        public long MaxStorageBytes { get; set; }
        public bool HasPassword { get; set; }
    }
}
