using EasyDisk.API.Filters;
using EasyDisk.Application.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyDisk.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/users")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserService _userService;
        public AdminUsersController(IAdminUserService userService) => _userService = userService;

        [HttpGet] public async Task<IActionResult> Get() => Ok(await _userService.GetAllUsersAsync());

        [HttpPost("{id}/toggle-ban")]
        [Audit("Admin.ToggleBan", "User")]
        public async Task<IActionResult> ToggleBan(string id)
        {
            await _userService.ToggleUserBanAsync(id);
            return Ok(new { message = "Status updated" });
        }
    }
}
