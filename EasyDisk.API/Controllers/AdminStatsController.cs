using EasyDisk.Application.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyDisk.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/stats")]
    public class AdminStatsController : ControllerBase
    {
        private readonly IAdminStatsService _statsService;
        public AdminStatsController(IAdminStatsService statsService) => _statsService = statsService;

        [HttpGet] public async Task<IActionResult> Get() => Ok(await _statsService.GetDashboardStatsAsync());
    }
}
