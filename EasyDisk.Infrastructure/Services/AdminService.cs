using EasyDisk.Application.DTOs;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Extensions;
using EasyDisk.Application.Interfaces;
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
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IEnumerable<UserDetailDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDetailDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isBanned = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

                userDtos.Add(new UserDetailDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "Unknown",
                    Roles = roles,
                    UsedQuotaBytes = user.UsedQuotaBytes,
                    IsBanned = isBanned
                });
            }

            return userDtos;
        }

        public async Task ToggleUserBanAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId).EnsureExistsAsync(() => $"User with id {userId} not found.");

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                throw new ValidationException("Cannot ban an admin user.");
            }

            var isCurrentlyBanned = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            if (isCurrentlyBanned)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }
        }
    }
}
