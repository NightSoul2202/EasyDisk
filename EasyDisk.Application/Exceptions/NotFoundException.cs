using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Exceptions
{
    public class NotFoundException : AppException
    {
        public NotFoundException(string message)
            : base(message)
        {
        }

        public NotFoundException(string name, object key) 
            : base($"Entitie '{name}' ({key}) not found.") 
        {
        }
    }
}
