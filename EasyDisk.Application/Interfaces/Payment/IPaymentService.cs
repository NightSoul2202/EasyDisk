using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces.Payment
{
    public interface IPaymentService 
    {
        Task ProcessWebhookAsync(string json, string signature);
    }
}
