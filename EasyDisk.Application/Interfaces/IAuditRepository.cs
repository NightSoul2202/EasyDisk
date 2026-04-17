using EasyDisk.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditLogEntity log);
        Task<IEnumerable<AuditLogEntity>> GetLatestLogsAsync(string? userId, int limit);
        Task SaveChangesAsync();
    }
}
