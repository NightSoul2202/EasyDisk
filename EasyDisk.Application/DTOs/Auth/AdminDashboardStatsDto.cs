using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.DTOs.Auth
{
    public class AdminDashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int BannedUsers { get; set; }
        public long TotalUsedStorage { get; set; }
        public long TotalAllocatedStorage { get; set; }
    }
}
