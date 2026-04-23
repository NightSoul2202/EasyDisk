using EasyDisk.API.Filters;
using EasyDisk.Application.Interfaces.Admin;
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

        [HttpPost("users/{id}/toggle-ban")]
        [Audit("Admin.ToggleBan", "User")]
        public async Task<IActionResult> ToggleUserBan(string id)
        {
            await _adminService.ToggleUserBanAsync(id);

            return Ok(new { message = "User ban status toggled successfully." });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _adminService.GetAllUsersAsync();

            return Ok(users);
        }

        [HttpGet]
        [Route("audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] string? userId = null)
        {
            var logs = await _adminService.GetAuditLogsAsync(userId);

            return Ok(logs);
        }

        [HttpGet]
        [Route("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _adminService.GetDashboardStatsAsync();

            return Ok(stats);
        }
    }
}
