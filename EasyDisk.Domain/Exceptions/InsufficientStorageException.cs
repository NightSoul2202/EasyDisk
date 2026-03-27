using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Domain.Exceptions
{
    public class InsufficientStorageException : DomainException
    {
        public InsufficientStorageException(long requestedBytes, long availableBytes) 
            : base($"Not enough space! Need: {requestedBytes} bytes, available: {availableBytes}.")
        {
        }
    }
}
