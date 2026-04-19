using EasyDisk.API.Filters;
using EasyDisk.Application.DTOs;
using EasyDisk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System.Security.Claims;

namespace EasyDisk.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            await _authService.RegisterAsync(registerDto);

            return Ok(new { message = "Register is success!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);

            return Ok(result);
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto googleLoginDto)
        {
            var result = await _authService.GoogleLoginAsync(googleLoginDto);

            return Ok(result);
        }

        [HttpPost]
        [Route("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _authService.ForgotPasswordAsync(dto);

            return Ok(new { message = "If an account with that address exists, we've sent it an email with instructions." });
        }

        [HttpPost]
        [Route("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _authService.ResetPasswordAsync(dto);

            return Ok(new { message = "Password changed successfully." });
        }

        [HttpPost]
        [Route("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
        {
            await _authService.ConfirmEmailAsync(dto.UserId, dto.Token);

            return Ok(new { message = "Email successfully verified." });
        }

        [HttpPost]
        [Route("resend-confirmation-email")]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendEmailDto dto)
        {
            await _authService.ResendConfirmationEmail(dto);

            return Ok(new { message = "If your email is registered, a new confirmation link has been sent." });
        }

        [Authorize]
        [HttpPost]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid token claims." });
            }

            var result = await _authService.RefreshTokenAsync(userId);
            return Ok(result);
        }
    }
}
