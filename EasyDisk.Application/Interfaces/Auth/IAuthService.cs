using EasyDisk.Application.DTOs;
using EasyDisk.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto);
        Task ConfirmEmailAsync(string userId, string token);
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
        Task ResendConfirmationEmail(ResendEmailDto resendEmailDto);
        Task<AuthResponseDto> RefreshTokenAsync(string userId);
        Task<TwoFactorSetupResponseDto> Get2FaSetupInfoAsync(string userId);
        Task Confirm2FaSetupAsync(string userId, string code);
        Task Disable2FaAsync(string userId);
    }
}
