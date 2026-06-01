using EasyDisk.Application.DTOs.Auth;
using EasyDisk.Application.Interfaces.Admin;
using EasyDisk.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class AdminStatsService : IAdminStatsService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public AdminStatsService(UserManager<ApplicationUser> userManager) => _userManager = userManager;

        public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            return new AdminDashboardStatsDto
            {
                TotalUsers = users.Count,
                BannedUsers = users.Count(u => u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow),
                TotalUsedStorage = users.Sum(u => u.UsedQuotaBytes),
                TotalAllocatedStorage = users.Sum(u => u.MaxStorageBytes)
            };
        }
    }
}
