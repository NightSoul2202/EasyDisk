using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Exceptions
{
    public class StorageOperationException : InfrastructureException
    {
        public StorageOperationException(string message) : base(message)
        {
        }

        public StorageOperationException(string filePath, Exception innerException) 
            : base($"Error while trying to write file to path: {filePath}", innerException)
        {
        }
    }
}
