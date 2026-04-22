using EasyDisk.Application.DTOs;
using EasyDisk.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces.Auth
{
    public interface IProfileService
    {
        Task UpdateProfileAsync(string userId, UpdateProfileDto dto);
        Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<ProfileResponseDto> GetProfileAsync(string userId);
    }
}
