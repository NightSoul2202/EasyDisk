using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Exceptions
{
    public abstract class InfrastructureException : Exception
    {
        protected InfrastructureException(string message)
            : base(message)
        {
        }
        protected InfrastructureException(string message, Exception innerException) 
            : base(message, innerException) 
        {
        }
    }
}
