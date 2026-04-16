using EasyDisk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyDisk.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _adminService.GetAllUsersAsync();

            return Ok(users);
        }

        [HttpPost("users/{userId}/toggle-ban")]
        public async Task<IActionResult> ToggleUserBan(string userId)
        {
            await _adminService.ToggleUserBanAsync(userId);

            return Ok(new { message = "User ban status toggled successfully." });
        }
    }
}
