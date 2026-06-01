using EasyDisk.Application.Interfaces.Auth;
using EasyDisk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDisk.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public UserRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task UpdateUserQuotaAsync(string userId, long sizeChange)
        {
            await _dbContext.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.UsedQuotaBytes, x => x.UsedQuotaBytes + sizeChange));
        }

        public async Task<(long UsedBytes, long MaxBytes)> GetUserQuotaInfoAsync(string userId)
        {
            var user = await _dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.UsedQuotaBytes, u.MaxStorageBytes })
                .FirstOrDefaultAsync();

            return (user?.UsedQuotaBytes ?? 0, user?.MaxStorageBytes ?? 0);
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }

    }
}
