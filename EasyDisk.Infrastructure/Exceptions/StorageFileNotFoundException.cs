using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Exceptions
{
    public class StorageFileNotFoundException : InfrastructureException
    {
        public StorageFileNotFoundException(string message) : base(message)
        {
        }
    }
}
