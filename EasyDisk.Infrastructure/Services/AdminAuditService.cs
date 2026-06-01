using EasyDisk.Application.DTOs.Auth;
using EasyDisk.Application.Interfaces.Admin;
using EasyDisk.Application.Interfaces.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class AdminAuditService : IAdminAuditService
    {
        private readonly IAuditRepository _auditRepository;
        public AdminAuditService(IAuditRepository auditRepository) => _auditRepository = auditRepository;

        public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(string? userId = null)
        {
            var logs = await _auditRepository.GetLatestLogsAsync(userId, 100);
            return logs.Select(l => new AuditLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                Action = l.Action,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Details = l.Details,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                IsSuccess = l.IsSuccess,
                Timestamp = l.Timestamp
            });
        }
    }
}
