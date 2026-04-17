using EasyDisk.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EasyDisk.API.Filters
{
    public class AuditLogFilter : IAsyncActionFilter
    {
        private readonly IAuditService _auditService;
        private readonly string _actionName;
        private readonly string _entityType;

        public AuditLogFilter(IAuditService auditService, string actionName, string entityType)
        {
            _auditService = auditService;
            _actionName = actionName;
            _entityType = entityType;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executedContext = await next();

            bool isSuccess = executedContext.Exception == null;

            var details = executedContext.Exception != null
                ? new { Error = executedContext.Exception.Message } 
                : null;

            await _auditService.LogAsync(
                action: _actionName,
                entityType: _entityType,
                entityId: context.RouteData.Values["id"]?.ToString(),
                details: details,
                isSuccess: isSuccess
            );
        }
    }
}
