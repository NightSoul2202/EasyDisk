using EasyDisk.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto);
        Task ConfirmEmailAsync(string userId, string token);
        Task<TwoFactorSetupResponseDto> Get2FaSetupInfoAsync(string userId);
        Task Confirm2FaSetupAsync(string userId, string code);
        Task Disable2FaAsync(string userId);
    }
}
