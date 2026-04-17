using Microsoft.AspNetCore.Mvc;
using System;

namespace EasyDisk.API.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AuditAttribute : TypeFilterAttribute
    {
        public AuditAttribute(string actionName, string entityType) : base(typeof(AuditLogFilter))
        {
            Arguments = new object[] { actionName, entityType };
        }
    }
}
