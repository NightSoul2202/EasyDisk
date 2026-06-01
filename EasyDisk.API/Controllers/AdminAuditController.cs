using EasyDisk.Application.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyDisk.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/audit")]
    public class AdminAuditController : ControllerBase
    {
        private readonly IAdminAuditService _auditService;
        public AdminAuditController(IAdminAuditService auditService) => _auditService = auditService;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? userId)
            => Ok(await _auditService.GetAuditLogsAsync(userId));
    }
}
