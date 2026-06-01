using EasyDisk.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces.Admin
{
    public interface IAdminAuditService
    {
        Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(string? userId = null);
    }
}
