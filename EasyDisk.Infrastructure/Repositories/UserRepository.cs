using EasyDisk.Application.Interfaces;
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

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }

    }
}
