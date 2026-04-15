using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Domain.Constants
{
    public static class SubscriptionPlans
    {
        public const string Free = "Free";
        public const string Basic = "Basic_100GB";
        public const string Pro = "Pro_500GB";
        public const string Premium = "Premium_1TB";

        public static long GetBytesForPlan(string planName)
        {
            return planName switch
            {
                Free => 20L * 1024 * 1024 * 1024,
                Basic => 100L * 1024 * 1024 * 1024,
                Pro => 500L * 1024 * 1024 * 1024,
                Premium => 1024L * 1024 * 1024 * 1024,
                _ => 20L * 1024 * 1024 * 1024
            };
        }
    }
}
