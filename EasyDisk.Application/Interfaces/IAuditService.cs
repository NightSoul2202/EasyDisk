using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityType, string? entityId, object? details, bool isSuccess = true);
    }
}
