using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces.Payment
{
    public interface ISubscriptionService
    {
        Task<string> CreateCheckoutSessionAsync(string userId, string planName);
        Task CancelSubscriptionAsync(string userId);
    }
}
