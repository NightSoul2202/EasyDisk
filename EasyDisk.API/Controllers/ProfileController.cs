using EasyDisk.Application.DTOs;
using EasyDisk.Application.Exceptions;
using EasyDisk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyDisk.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuthService _authService;
        private readonly IProfileService _profileService;

        public ProfileController(ICurrentUserService currentUserService, IAuthService authService, IProfileService profileService)
        {
            _currentUserService = currentUserService;
            _authService = authService;
            _profileService = profileService;
        }

        [HttpPost]
        [Route("2fa/confirm")]
        public async Task<IActionResult> ConfirmTwoFactorAuth([FromBody] TwoFactorConfirmDto confirmDto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to confirm two-factor authentication.");

            await _authService.Confirm2FaSetupAsync(userId, confirmDto.Code);

            return Ok(new { message = "Two-factor authentication confirmed successfully." });
        }

        [HttpPost]
        [Route("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to change password.");

            await _profileService.ChangePasswordAsync(userId, changePasswordDto);

            return Ok(new { message = "Password changed successfully." });
        }

        [HttpGet]
        [Route("2fa/setup")]
        public async Task<IActionResult> GetTwoFactorSetup()
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to get two-factor authentication setup info.");

            var setupInfo = await _authService.Get2FaSetupInfoAsync(userId);

            return Ok(setupInfo);
        }

        [HttpGet]
        [Route("get-profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to get profile information.");

            var profile = await _profileService.GetProfileAsync(userId);

            return Ok(profile);
        }

        [HttpPut]
        [Route("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to update profile information.");

            await _profileService.UpdateProfileAsync(userId, updateProfileDto);

            return Ok(new { message = "Profile updated successfully." });
        }

        [HttpDelete]
        [Route("2fa/disable")]
        public async Task<IActionResult> DisableTwoFactorAuth()
        {
            var userId = _currentUserService.UserId ?? throw new ValidationException("User must be authenticated to disable two-factor authentication.");

            await _authService.Disable2FaAsync(userId);

            return Ok(new { message = "Two-factor authentication disabled successfully." });
        }
    }
}
