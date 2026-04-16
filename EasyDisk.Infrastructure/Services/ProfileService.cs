using EasyDisk.Application.DTOs;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Interfaces;
using EasyDisk.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new NotFoundException("User", userId);

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Помилка зміни пароля.";
                throw new ValidationException(error);
            }
        }

        public async Task UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new NotFoundException("User", userId);

            if (!string.IsNullOrWhiteSpace(dto.UserName))
            {
                user.UserName = dto.UserName;
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                user.PhoneNumber = dto.PhoneNumber;
            }

            if (dto.DateOfBirth.HasValue)
            {
                user.BirthDate = dto.DateOfBirth.Value;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new ValidationException("Error update profile.");
            }
        }

        public async Task<ProfileResponseDto> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new NotFoundException("User", userId);

            return new ProfileResponseDto
            {
                Username = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                SubscriptionPlan = user.SubscriptionPlan,
                BirthDate = user.BirthDate,
                TwoFactorEnabled = user.TwoFactorEnabled,
                UsedQuotaBytes = user.UsedQuotaBytes,
                MaxStorageBytes = user.MaxStorageBytes
            };
        }
    }
}
