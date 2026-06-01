using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Application.Interfaces.Auth
{
    public interface IUserRepository
    {
        Task UpdateUserQuotaAsync(string userId, long sizeChange);
        Task<(long UsedBytes, long MaxBytes)> GetUserQuotaInfoAsync(string userId);
        Task SaveChangesAsync();
    }
}
