using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Exceptions
{
    public abstract class AppException : Exception
    {
        protected AppException() 
            : base() 
        {
        }

        protected AppException(string message) 
            : base(message) 
        {
        }

        protected AppException(string message, params object[] args) 
            : base(string.Format(CultureInfo.CurrentCulture, message, args)) 
        {
        }
    }
}
