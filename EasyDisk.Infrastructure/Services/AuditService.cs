using EasyDisk.Application.Interfaces;
using EasyDisk.Domain.Entities;
using EasyDisk.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditRepository _auditRepository;

        public AuditService(
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ICurrentUserService currentUserService,
            IAuditRepository auditRepository)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _currentUserService = currentUserService;
            _auditRepository = auditRepository;
        }

        public async Task LogAsync(string action, string entityType, string? entityId, object? details, bool isSuccess = true)
        {
            var context = _httpContextAccessor.HttpContext;

            var ipAddress = context?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown IP";
            var userAgent = context?.Request?.Headers["User-Agent"].ToString() ?? "Unknown Device";

            var userId = _currentUserService.UserId;

            var auditLog = new AuditLogEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details != null ? JsonSerializer.Serialize(details) : "{}",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = isSuccess,
                Timestamp = DateTime.UtcNow
            };

            await _auditRepository.AddAsync(auditLog);
            await _auditRepository.SaveChangesAsync();
        }
    }
}
