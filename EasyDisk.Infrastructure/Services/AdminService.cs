using EasyDisk.Application.DTOs.Auth;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Extensions;
using EasyDisk.Application.Interfaces.Admin;
using EasyDisk.Application.Interfaces.Audit;
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
        private readonly IAuditRepository _auditRepository;

        public AdminService(UserManager<ApplicationUser> userManager, IAuditRepository auditRepository)
        {
            _userManager = userManager;
            _auditRepository = auditRepository;
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

        public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(string? userId = null)
        {
            var logs = await _auditRepository.GetLatestLogsAsync(userId, 100);

            return logs.Select(l => new AuditLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                Action = l.Action,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Details = l.Details,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                IsSuccess = l.IsSuccess,
                Timestamp = l.Timestamp
            });
        }
    }
}
